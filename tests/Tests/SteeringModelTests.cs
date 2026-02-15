namespace Tests;

public class SteeringModelTests
{
    [Fact]
    public void NoInput_AngleReturnsToCentre()
    {
        var result = SteeringModel.Compute(false, false, 0.3f, 5f, true);
        Assert.Equal(0.3f - CarConstants.SteerAngleStep, result.SteerAngle, 0.001f);
    }

    [Fact]
    public void NoInput_ZeroAngleStaysZero()
    {
        var result = SteeringModel.Compute(false, false, 0f, 5f, true);
        Assert.Equal(0f, result.SteerAngle, 0.001f);
    }

    [Fact]
    public void Left_IncreasesAngle()
    {
        var result = SteeringModel.Compute(true, false, 0f, 5f, true);
        Assert.Equal(CarConstants.SteerAngleStep, result.SteerAngle, 0.001f);
    }

    [Fact]
    public void Right_DecreasesAngle()
    {
        var result = SteeringModel.Compute(false, true, 0f, 5f, true);
        Assert.Equal(-CarConstants.SteerAngleStep, result.SteerAngle, 0.001f);
    }

    [Fact]
    public void BothInputs_AngleStaysZero()
    {
        // Left and right cancel out — target is 0
        var result = SteeringModel.Compute(true, true, 0f, 5f, true);
        Assert.Equal(0f, result.SteerAngle, 0.001f);
    }

    [Fact]
    public void ZeroSpeed_ZeroYaw()
    {
        var result = SteeringModel.Compute(true, false, CarConstants.MaxSteerAngle, 0f, true);
        Assert.Equal(0f, result.YawSpeed, 0.001f);
    }

    [Fact]
    public void Airborne_ZeroYaw()
    {
        var result = SteeringModel.Compute(true, false, 0.3f, 10f, false);
        Assert.Equal(0f, result.YawSpeed, 0.001f);
    }

    [Fact]
    public void Airborne_AngleStillUpdates()
    {
        var result = SteeringModel.Compute(true, false, 0f, 10f, false);
        Assert.Equal(CarConstants.SteerAngleStep, result.SteerAngle, 0.001f);
    }

    [Fact]
    public void YawRate_MatchesBicycleModel()
    {
        float angle = 0.3f;
        float speed = 8f;
        var result = SteeringModel.Compute(false, false, angle, speed, true);

        // Angle decreased by one step since no input
        float actualAngle = angle - CarConstants.SteerAngleStep;
        float expected = (speed * System.MathF.Tan(actualAngle)) / CarConstants.Wheelbase;
        Assert.Equal(expected, result.YawSpeed, 0.01f);
    }
}
