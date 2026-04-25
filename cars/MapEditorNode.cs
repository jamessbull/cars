using Godot;

/// <summary>
/// Root node for the standalone map editor scene (editor.tscn).
/// Builds the entire scene graph in _Ready() — no .tscn child nodes required.
/// </summary>
public partial class MapEditorNode : Node3D
{
    private const int SidebarWidth  = 180;
    private const int TopBarHeight  = 40;

    private MapEditorState       _state      = new();
    private Camera3D             _camera;
    private GridMap              _gridMap;
    private MeshInstance3D       _ghostTile;
    private MeshInstance3D       _gridPlane;
    private MeshInstance3D       _cellOutline;
    private Label                _heightLabel;
    private Label                _facingLabel;
    private Label                _saveLabel;
    private Panel                _sidebarPanel;
    private Button[]             _tileButtons;
    private Timer                _saveTimer;

    // ─── Earth block tool state ───────────────────────────────────────────────
    private bool                 _earthMode;
    private bool                 _earthDragActive;
    private (int gx, int gz)     _earthDragStart;
    private (int gx, int gz)     _earthDragCurrent;
    private MeshInstance3D       _earthPreview;
    private Node3D               _earthContainer;
    private System.Collections.Generic.Dictionary<(int, int), BlockOfEarth> _earthBlockNodes = new();
    private Button               _earthModeBtn;

    // ─── Camera orbit / pan / zoom state ─────────────────────────────────────
    private Vector3 _camPivot  = Vector3.Zero;
    private float   _camYaw    = 45f;   // degrees, horizontal rotation
    private float   _camPitch  = 35.26f; // degrees, elevation
    private float   _camZoom   = 30f;   // orthographic size reference (world units at 648 px tall)
    private bool    _orbitActive;

    public override void _Ready()
    {
        SetupEnvironment();
        SetupCamera();
        SetupLight();
        SetupGridMap();
        SetupGhostTile();
        SetupGridPlane();
        SetupCellOutline();
        SetupUI();
        SetupEarthContainer();
        SetupEarthPreview();
        LoadExistingLayout();
        UpdateUIState();
    }

    // ─── Scene setup ──────────────────────────────────────────────────────────

    private void SetupEnvironment()
    {
        var env = new Environment();
        env.AmbientLightSource  = Environment.AmbientSource.Color;
        env.AmbientLightColor   = new Color(0.4f, 0.4f, 0.4f);
        env.AmbientLightEnergy  = 1.0f;
        env.BackgroundMode      = Environment.BGMode.Color;
        env.BackgroundColor     = new Color(0.18f, 0.18f, 0.22f);

        var worldEnv = new WorldEnvironment();
        worldEnv.Environment = env;
        AddChild(worldEnv);
    }

    private void SetupCamera()
    {
        _camera = new Camera3D();
        _camera.Projection = Camera3D.ProjectionType.Orthogonal;
        AddChild(_camera);
        GetViewport().SizeChanged += UpdateCameraSize;
        UpdateCameraSize();
        ApplyCameraTransform();
    }

    private void UpdateCameraSize()
    {
        // Keep the same world-units-per-pixel regardless of window size.
        // _camZoom is the reference size at 648 px tall; zoom in/out adjusts _camZoom.
        float h = GetViewport().GetVisibleRect().Size.Y;
        _camera.Size = _camZoom * h / 648f;
    }

    /// <summary>Rebuild the camera position from yaw/pitch/zoom/pivot.</summary>
    private void ApplyCameraTransform()
    {
        float yawRad   = _camYaw   * System.MathF.PI / 180f;
        float pitchRad = _camPitch * System.MathF.PI / 180f;
        const float Dist = 52f; // fixed world-space distance; zoom uses orthographic Size

        float x = Dist * System.MathF.Cos(pitchRad) * System.MathF.Sin(yawRad);
        float y = Dist * System.MathF.Sin(pitchRad);
        float z = Dist * System.MathF.Cos(pitchRad) * System.MathF.Cos(yawRad);

        _camera.Position = _camPivot + new Vector3(x, y, z);
        _camera.LookAt(_camPivot, Vector3.Up);
        UpdateGridPlaneY(); // keep grid plane centred under camera pivot
    }

    /// <summary>Camera right direction projected flat onto XZ.</summary>
    private Vector3 CameraRightXZ()
    {
        float yawRad = _camYaw * System.MathF.PI / 180f;
        return new Vector3(System.MathF.Cos(yawRad), 0f, -System.MathF.Sin(yawRad));
    }

    /// <summary>Camera forward direction projected flat onto XZ.</summary>
    private Vector3 CameraForwardXZ()
    {
        float yawRad = _camYaw * System.MathF.PI / 180f;
        return new Vector3(-System.MathF.Sin(yawRad), 0f, -System.MathF.Cos(yawRad));
    }

    private void SetupLight()
    {
        var light = new DirectionalLight3D();
        light.Rotation = new Vector3(-System.MathF.PI / 4f, System.MathF.PI / 4f, 0f);
        light.LightEnergy = 1.0f;
        AddChild(light);
    }

    private void SetupGridMap()
    {
        _gridMap = new GridMap();
        _gridMap.MeshLibrary = TileMeshBuilder.BuildMeshLibrary();
        _gridMap.CellSize    = new Vector3(TileGeometry.CellWidth, TileGeometry.CellHeight, TileGeometry.CellDepth);
        AddChild(_gridMap);
    }

    private void SetupGhostTile()
    {
        _ghostTile = new MeshInstance3D();
        AddChild(_ghostTile);
        RebuildGhostMesh();
    }

    private void SetupGridPlane()
    {
        var planeMesh  = new PlaneMesh();
        planeMesh.Size = new Vector2(200f, 200f);

        var planeMat             = new StandardMaterial3D();
        planeMat.AlbedoColor     = new Color(0.4f, 0.6f, 1.0f, 0.12f);
        planeMat.Transparency    = BaseMaterial3D.TransparencyEnum.Alpha;
        planeMat.CullMode        = BaseMaterial3D.CullModeEnum.Disabled;
        planeMat.ShadingMode     = BaseMaterial3D.ShadingModeEnum.Unshaded;

        _gridPlane               = new MeshInstance3D();
        _gridPlane.Mesh          = planeMesh;
        _gridPlane.MaterialOverride = planeMat;
        AddChild(_gridPlane);
        UpdateGridPlaneY();
    }

    private void SetupCellOutline()
    {
        float hw = TileGeometry.CellWidth  / 2f;
        float hd = TileGeometry.CellDepth  / 2f;

        // 4 line segments (8 vertices) forming a CellWidth × CellDepth rectangle in the XZ plane
        var vertices = new Vector3[]
        {
            new(-hw, 0f, -hd), new( hw, 0f, -hd),
            new( hw, 0f, -hd), new( hw, 0f,  hd),
            new( hw, 0f,  hd), new(-hw, 0f,  hd),
            new(-hw, 0f,  hd), new(-hw, 0f, -hd),
        };

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;

        var outlineMesh = new ArrayMesh();
        outlineMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrays);

        var mat = new StandardMaterial3D();
        mat.AlbedoColor  = new Color(1f, 0.85f, 0f);
        mat.ShadingMode  = BaseMaterial3D.ShadingModeEnum.Unshaded;
        mat.NoDepthTest  = true;

        _cellOutline                = new MeshInstance3D();
        _cellOutline.Mesh           = outlineMesh;
        _cellOutline.MaterialOverride = mat;
        _cellOutline.Visible        = false;
        AddChild(_cellOutline);
    }

    private void SetupUI()
    {
        var canvasLayer = new CanvasLayer();
        AddChild(canvasLayer);

        // ── Left sidebar ──────────────────────────────────────────────────
        _sidebarPanel = new Panel();
        _sidebarPanel.SetAnchor(Side.Left,   0);
        _sidebarPanel.SetAnchor(Side.Right,  0);
        _sidebarPanel.SetAnchor(Side.Top,    0);
        _sidebarPanel.SetAnchor(Side.Bottom, 1);
        _sidebarPanel.SetOffset(Side.Left,   0);
        _sidebarPanel.SetOffset(Side.Right,  SidebarWidth);
        _sidebarPanel.SetOffset(Side.Top,    TopBarHeight);
        _sidebarPanel.SetOffset(Side.Bottom, 0);
        canvasLayer.AddChild(_sidebarPanel);

        var vbox        = new VBoxContainer();
        vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 4);
        _sidebarPanel.AddChild(vbox);

        var tilesLabel  = new Label();
        tilesLabel.Text = "Tiles";
        vbox.AddChild(tilesLabel);

        var tileTypes   = System.Enum.GetValues<TileType>();
        _tileButtons    = new Button[tileTypes.Length];
        for (int i = 0; i < tileTypes.Length; i++)
        {
            var type    = tileTypes[i];
            var btn     = new Button();
            btn.Text    = type.ToString();
            btn.Pressed += () =>
            {
                _state.SelectTileType(type);
                UpdateUIState();
            };
            vbox.AddChild(btn);
            _tileButtons[i] = btn;
        }

        vbox.AddChild(new HSeparator());

        var worldLabel  = new Label();
        worldLabel.Text = "World";
        vbox.AddChild(worldLabel);

        _earthModeBtn       = new Button();
        _earthModeBtn.Text  = "Earth Blocks";
        _earthModeBtn.Pressed += () =>
        {
            _earthMode = !_earthMode;
            _earthModeBtn.Modulate = _earthMode ? new Color(0.85f, 0.55f, 0.20f) : Colors.White;
        };
        vbox.AddChild(_earthModeBtn);

        // ── Top bar ───────────────────────────────────────────────────────
        var topBarPanel = new Panel();
        topBarPanel.SetAnchor(Side.Left,   0);
        topBarPanel.SetAnchor(Side.Right,  1);
        topBarPanel.SetAnchor(Side.Top,    0);
        topBarPanel.SetAnchor(Side.Bottom, 0);
        topBarPanel.SetOffset(Side.Left,   0);
        topBarPanel.SetOffset(Side.Right,  0);
        topBarPanel.SetOffset(Side.Top,    0);
        topBarPanel.SetOffset(Side.Bottom, TopBarHeight);
        canvasLayer.AddChild(topBarPanel);

        var hbox = new HBoxContainer();
        hbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        hbox.AddThemeConstantOverride("separation", 16);
        topBarPanel.AddChild(hbox);

        // Spacer so labels are not behind sidebar
        var spacer       = new Control();
        spacer.CustomMinimumSize = new Vector2(SidebarWidth + 8, 0);
        hbox.AddChild(spacer);

        _heightLabel     = new Label();
        _heightLabel.Text = "Y: 0";
        _heightLabel.VerticalAlignment = VerticalAlignment.Center;
        hbox.AddChild(_heightLabel);

        _facingLabel     = new Label();
        _facingLabel.Text = "Facing: North";
        _facingLabel.VerticalAlignment = VerticalAlignment.Center;
        hbox.AddChild(_facingLabel);

        var saveBtn      = new Button();
        saveBtn.Text     = "Save";
        saveBtn.Pressed  += OnSavePressed;
        hbox.AddChild(saveBtn);

        _saveLabel       = new Label();
        _saveLabel.Text  = "Saved!";
        _saveLabel.Visible = false;
        _saveLabel.VerticalAlignment = VerticalAlignment.Center;
        hbox.AddChild(_saveLabel);

        // Timer for "Saved!" auto-hide
        _saveTimer            = new Timer();
        _saveTimer.WaitTime   = 2.0;
        _saveTimer.OneShot    = true;
        _saveTimer.Timeout   += () => _saveLabel.Visible = false;
        AddChild(_saveTimer);

        // ── Help bar (bottom) ─────────────────────────────────────────────
        var helpPanel = new Panel();
        helpPanel.SetAnchor(Side.Left,   0);
        helpPanel.SetAnchor(Side.Right,  1);
        helpPanel.SetAnchor(Side.Top,    1);
        helpPanel.SetAnchor(Side.Bottom, 1);
        helpPanel.SetOffset(Side.Top,    -28);
        helpPanel.SetOffset(Side.Bottom, 0);
        canvasLayer.AddChild(helpPanel);

        var helpLabel = new Label();
        helpLabel.Text =
            "Scroll: height   Ctrl+Scroll: tile type   Shift+Scroll: facing   " +
            "LMB: place   RMB: delete   Middle-drag: orbit   Arrows: pan   = / −: zoom   " +
            "Selecting Ramp advances Y by " + TileGeometry.ArcGridSpan +
            ";  RampExit by " + TileGeometry.RampGridSpan;
        helpLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        helpLabel.VerticalAlignment   = VerticalAlignment.Center;
        helpLabel.HorizontalAlignment = HorizontalAlignment.Center;
        helpPanel.AddChild(helpLabel);
    }

    private void SetupEarthContainer()
    {
        _earthContainer = new Node3D();
        AddChild(_earthContainer);
    }

    private void SetupEarthPreview()
    {
        var planeMesh = new PlaneMesh();
        planeMesh.Size = new Vector2(TileGeometry.CellWidth, TileGeometry.CellDepth);

        var mat = new StandardMaterial3D();
        mat.AlbedoColor  = new Color(0.55f, 0.33f, 0.12f, 0.45f);
        mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        mat.ShadingMode  = BaseMaterial3D.ShadingModeEnum.Unshaded;
        mat.CullMode     = BaseMaterial3D.CullModeEnum.Disabled;

        _earthPreview = new MeshInstance3D();
        _earthPreview.Mesh            = planeMesh;
        _earthPreview.MaterialOverride = mat;
        _earthPreview.Visible         = false;
        AddChild(_earthPreview);
    }

    // ─── Load existing track layout ──────────────────────────────────────────

    private void LoadExistingLayout()
    {
        if (!FileAccess.FileExists("res://track_layout.json")) return;

        var json = FileAccess.GetFileAsString("res://track_layout.json");
        TilePlacement[] placements;
        try
        {
            placements = TrackLayoutLoader.LoadFromJson(json);
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"MapEditor: failed to load track_layout.json: {e.Message}");
            return;
        }

        foreach (var p in placements)
        {
            _state.PlacedTiles[(p.GridX, p.GridY, p.GridZ)] = p;
            int tileId      = (int)p.Type;
            int orientation = TrackLayout.GetOrientationIndex(p.Facing);
            _gridMap.SetCellItem(new Vector3I(p.GridX, p.GridY, p.GridZ), tileId, orientation);
        }

        GD.Print($"MapEditor: loaded {placements.Length} tiles from track_layout.json");

        try
        {
            var earthPlacements = TrackLayoutLoader.LoadEarthBlocksFromJson(json);
            foreach (var eb in earthPlacements)
            {
                _state.PlaceEarthBlock(eb.GridX, eb.GridZ);
                var key = (eb.GridX, eb.GridZ);
                if (!_earthBlockNodes.ContainsKey(key))
                {
                    var block = new BlockOfEarth();
                    block.Position = BlockOfEarth.GridToWorld(eb.GridX, eb.GridZ);
                    _earthContainer.AddChild(block);
                    _earthBlockNodes[key] = block;
                }
            }
            if (earthPlacements.Length > 0)
                GD.Print($"MapEditor: loaded {earthPlacements.Length} earth blocks");
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"MapEditor: failed to load earth blocks: {e.Message}");
        }
    }

    // ─── Per-frame update ────────────────────────────────────────────────────

    public override void _Process(double delta)
    {
        HandleCameraMovement((float)delta);

        var mouse      = GetViewport().GetMousePosition();
        bool overUI    = IsMouseOverSidebar(mouse);

        _ghostTile.Visible   = !overUI;
        _cellOutline.Visible = !overUI;

        if (!overUI)
        {
            var (gx, gz) = GetHoveredCell(mouse);
            var ghostPos = GridToWorld(gx, _state.GridY, gz);
            _ghostTile.Position  = ghostPos;
            _cellOutline.Position = new Vector3(ghostPos.X, TileGeometry.CellHeight / 2f, ghostPos.Z);
        }
    }

    private void HandleCameraMovement(float delta)
    {
        const float PanSpeed  = 15f;  // world units / second
        const float ZoomSpeed = 0.8f; // zoom factor / second (multiplicative)

        bool moved = false;

        // Pan with arrow keys along the camera's XZ axes
        if (Input.IsKeyPressed(Key.Left))  { _camPivot -= CameraRightXZ()   * (PanSpeed * delta); moved = true; }
        if (Input.IsKeyPressed(Key.Right)) { _camPivot += CameraRightXZ()   * (PanSpeed * delta); moved = true; }
        if (Input.IsKeyPressed(Key.Up))    { _camPivot += CameraForwardXZ() * (PanSpeed * delta); moved = true; }
        if (Input.IsKeyPressed(Key.Down))  { _camPivot -= CameraForwardXZ() * (PanSpeed * delta); moved = true; }

        // Zoom with = (zoom in) and - (zoom out); also numpad + / -
        if (Input.IsKeyPressed(Key.Equal)       || Input.IsKeyPressed(Key.KpAdd))
        { _camZoom = System.MathF.Max(2f, _camZoom * (1f - ZoomSpeed * delta)); moved = true; }
        if (Input.IsKeyPressed(Key.Minus)       || Input.IsKeyPressed(Key.KpSubtract))
        { _camZoom = System.MathF.Min(200f, _camZoom * (1f + ZoomSpeed * delta)); moved = true; }

        if (moved) { UpdateCameraSize(); ApplyCameraTransform(); }
    }

    // ─── Input ───────────────────────────────────────────────────────────────

    public override void _Input(InputEvent @event)
    {
        // Middle mouse: toggle orbit
        if (@event is InputEventMouseButton orbitBtn && orbitBtn.ButtonIndex == MouseButton.Middle)
        {
            _orbitActive = orbitBtn.Pressed;
            return;
        }

        // Mouse motion: orbit OR update earth drag preview
        if (@event is InputEventMouseMotion motion)
        {
            if (_orbitActive)
            {
                _camYaw   -= motion.Relative.X * 0.4f;
                _camPitch  = System.MathF.Max(5f, System.MathF.Min(85f, _camPitch + motion.Relative.Y * 0.4f));
                ApplyCameraTransform();
            }
            else if (_earthMode && _earthDragActive)
            {
                _earthDragCurrent = GetHoveredCellAtWorldY(GetViewport().GetMousePosition(), 0f);
                UpdateEarthPreview();
            }
            return;
        }

        if (@event is not InputEventMouseButton mouseBtn) return;

        var mouse   = GetViewport().GetMousePosition();
        bool overUI = IsMouseOverSidebar(mouse);

        // ── Earth block mode ──────────────────────────────────────────────────
        if (_earthMode)
        {
            if (mouseBtn.ButtonIndex == MouseButton.Left && !overUI)
            {
                if (mouseBtn.Pressed)
                {
                    _earthDragActive  = true;
                    _earthDragStart   = GetHoveredCellAtWorldY(mouse, 0f);
                    _earthDragCurrent = _earthDragStart;
                    _earthPreview.Visible = true;
                    UpdateEarthPreview();
                }
                else if (_earthDragActive)
                {
                    CommitEarthRect();
                    _earthDragActive      = false;
                    _earthPreview.Visible = false;
                }
            }
            return;
        }

        // ── Tile placement mode ───────────────────────────────────────────────
        if (!mouseBtn.Pressed) return;

        switch (mouseBtn.ButtonIndex)
        {
            case MouseButton.Left when !overUI:
                PlaceTileAtMouse(mouse);
                break;

            case MouseButton.Right when !overUI:
                DeleteTileAtMouse(mouse);
                break;

            case MouseButton.WheelUp:
                if (Input.IsKeyPressed(Key.Ctrl))
                    _state.CycleTileType(-1);
                else if (Input.IsKeyPressed(Key.Shift))
                    _state.CycleFacing(-1);
                else
                    _state.ChangeGridY(MapEditorState.HeightStep);
                UpdateUIState();
                break;

            case MouseButton.WheelDown:
                if (Input.IsKeyPressed(Key.Ctrl))
                    _state.CycleTileType(1);
                else if (Input.IsKeyPressed(Key.Shift))
                    _state.CycleFacing(1);
                else
                    _state.ChangeGridY(-MapEditorState.HeightStep);
                UpdateUIState();
                break;
        }
    }

    // ─── Tile placement helpers ──────────────────────────────────────────────

    private void PlaceTileAtMouse(Vector2 mouse)
    {
        var (gx, gz) = GetHoveredCell(mouse);
        _state.PlaceTile(gx, _state.GridY, gz);
        int tileId      = (int)_state.SelectedType;
        int orientation = TrackLayout.GetOrientationIndex(_state.SelectedFacing);
        _gridMap.SetCellItem(new Vector3I(gx, _state.GridY, gz), tileId, orientation);
    }

    private void DeleteTileAtMouse(Vector2 mouse)
    {
        var (gx, gz) = GetHoveredCell(mouse);

        // Prefer the tile at the current grid height; if none exists there, fall back to
        // whichever tile in the same XZ column has the Y closest to the current height.
        int targetY = _state.GridY;
        if (!_state.PlacedTiles.ContainsKey((gx, targetY, gz)))
        {
            int bestDist = int.MaxValue;
            foreach (var key in _state.PlacedTiles.Keys)
            {
                if (key.x != gx || key.z != gz) continue;
                int dist = System.Math.Abs(key.y - _state.GridY);
                if (dist < bestDist) { bestDist = dist; targetY = key.y; }
            }
        }

        _state.DeleteTile(gx, targetY, gz);
        _gridMap.SetCellItem(new Vector3I(gx, targetY, gz), -1, 0);
    }

    // ─── Save ────────────────────────────────────────────────────────────────

    private void OnSavePressed()
    {
        var json = _state.ToJson();
        using var file = FileAccess.Open("res://track_layout.json", FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PrintErr("MapEditor: could not open track_layout.json for writing");
            return;
        }
        file.StoreString(json);
        GD.Print("MapEditor: saved track_layout.json");

        _saveLabel.Visible = true;
        _saveTimer.Start();
    }

    // ─── UI helpers ──────────────────────────────────────────────────────────

    private void UpdateUIState()
    {
        _heightLabel.Text = $"Y: {_state.GridY}";
        _facingLabel.Text = $"Facing: {_state.SelectedFacing}";

        UpdateGridPlaneY();
        RebuildGhostMesh();

        var types = System.Enum.GetValues<TileType>();
        for (int i = 0; i < _tileButtons.Length; i++)
        {
            _tileButtons[i].Modulate = types[i] == _state.SelectedType
                ? new Color(1f, 0.9f, 0.3f)
                : Colors.White;
        }
    }

    private void RebuildGhostMesh()
    {
        var meshData = TileGeometry.GenerateTile(_state.SelectedType);

        // Show the real tile material (game colors) with a blue emission glow so the
        // ghost is clearly distinguishable from placed tiles while looking authentic.
        var baseMat   = (StandardMaterial3D)TileMeshBuilder.MaterialForType(_state.SelectedType);
        var ghostMat  = new StandardMaterial3D();
        ghostMat.AlbedoColor            = baseMat.AlbedoColor;
        ghostMat.Roughness              = baseMat.Roughness;
        ghostMat.CullMode               = BaseMaterial3D.CullModeEnum.Disabled;
        ghostMat.EmissionEnabled        = true;
        ghostMat.Emission               = new Color(0.2f, 0.5f, 1.0f);
        ghostMat.EmissionEnergyMultiplier = 0.6f;

        _ghostTile.Mesh = TileMeshBuilder.BuildMesh(meshData, ghostMat);

        float rotY = _state.SelectedFacing switch
        {
            CardinalDirection.North => 0f,
            CardinalDirection.East  => -System.MathF.PI / 2f,
            CardinalDirection.South => System.MathF.PI,
            CardinalDirection.West  => System.MathF.PI / 2f,
            _                       => 0f
        };
        _ghostTile.Rotation = new Vector3(0f, rotY, 0f);
    }

    private void UpdateGridPlaneY()
    {
        if (_gridPlane == null) return;
        float y = _state.GridY * TileGeometry.CellHeight + TileGeometry.CellHeight / 2f;
        _gridPlane.Position = new Vector3(_camPivot.X, y, _camPivot.Z);
    }

    // ─── Earth block helpers ──────────────────────────────────────────────────

    private void CommitEarthRect()
    {
        int x0 = System.Math.Min(_earthDragStart.gx, _earthDragCurrent.gx);
        int x1 = System.Math.Max(_earthDragStart.gx, _earthDragCurrent.gx);
        int z0 = System.Math.Min(_earthDragStart.gz, _earthDragCurrent.gz);
        int z1 = System.Math.Max(_earthDragStart.gz, _earthDragCurrent.gz);

        int grassId       = (int)TileType.Grass;
        int grassOrient   = TrackLayout.GetOrientationIndex(CardinalDirection.North);

        for (int gx = x0; gx <= x1; gx++)
        for (int gz = z0; gz <= z1; gz++)
        {
            // Earth block below ground
            _state.PlaceEarthBlock(gx, gz);
            var key = (gx, gz);
            if (!_earthBlockNodes.ContainsKey(key))
            {
                var block = new BlockOfEarth();
                block.Position = BlockOfEarth.GridToWorld(gx, gz);
                _earthContainer.AddChild(block);
                _earthBlockNodes[key] = block;
            }

            // Grass tile on top (GridY = 0, same level as track)
            _state.PlaceTileDirect(gx, 0, gz, TileType.Grass, CardinalDirection.North);
            _gridMap.SetCellItem(new Vector3I(gx, 0, gz), grassId, grassOrient);
        }
    }

    private void UpdateEarthPreview()
    {
        int x0 = System.Math.Min(_earthDragStart.gx, _earthDragCurrent.gx);
        int x1 = System.Math.Max(_earthDragStart.gx, _earthDragCurrent.gx);
        int z0 = System.Math.Min(_earthDragStart.gz, _earthDragCurrent.gz);
        int z1 = System.Math.Max(_earthDragStart.gz, _earthDragCurrent.gz);

        float sx = (x1 - x0 + 1) * TileGeometry.CellWidth;
        float sz = (z1 - z0 + 1) * TileGeometry.CellDepth;
        float cx = x0 * TileGeometry.CellWidth  + sx / 2f;
        float cz = z0 * TileGeometry.CellDepth  + sz / 2f;

        ((PlaneMesh)_earthPreview.Mesh).Size = new Vector2(sx, sz);
        _earthPreview.Position = new Vector3(cx, 0.02f, cz);
    }

    // ─── Geometry helpers ────────────────────────────────────────────────────

    private bool IsMouseOverSidebar(Vector2 mouse)
    {
        return _sidebarPanel.GetGlobalRect().HasPoint(mouse);
    }

    private (int gx, int gz) GetHoveredCell(Vector2 mouse)
    {
        float planeY = _state.GridY * TileGeometry.CellHeight + TileGeometry.CellHeight / 2f;
        return GetHoveredCellAtWorldY(mouse, planeY);
    }

    /// <summary>Ray-intersect a horizontal plane at worldY and return the grid cell hit.</summary>
    private (int gx, int gz) GetHoveredCellAtWorldY(Vector2 mouse, float worldY)
    {
        Vector3 origin = _camera.ProjectRayOrigin(mouse);
        Vector3 dir    = _camera.ProjectRayNormal(mouse);
        if (System.MathF.Abs(dir.Y) < 0.0001f) return (0, 0);
        float t        = (worldY - origin.Y) / dir.Y;
        Vector3 world  = origin + t * dir;
        return MapEditorState.WorldToGridXZ(world.X, world.Z);
    }

    /// <summary>
    /// World position of a GridMap cell's item origin, matching Godot's default
    /// cell_center_x/y/z = true: each axis offset by half a cell.
    /// </summary>
    private static Vector3 GridToWorld(int gx, int gy, int gz)
    {
        return new Vector3(
            gx * TileGeometry.CellWidth  + TileGeometry.CellWidth  / 2f,
            gy * TileGeometry.CellHeight + TileGeometry.CellHeight / 2f,
            gz * TileGeometry.CellDepth  + TileGeometry.CellDepth  / 2f
        );
    }
}
