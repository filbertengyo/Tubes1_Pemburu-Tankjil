using System;
using System.Collections.Generic;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class DrinkAndDrive : Bot
{
    struct BotInfoData
    {
        public double X;
        public double Y;
        public double Energy;  
    }

    private List<BotInfoData> opponents;
    private BotInfoData? currentTarget = null; 
    private bool isHitBot = false; 
    private double currentAngle = 0;  

    static void Main(string[] args)
    {
        new DrinkAndDrive().Start();
    }

    DrinkAndDrive() : base(BotInfo.FromFile("DrinkAndDrive.json")) 
    {
        opponents = new List<BotInfoData>();
    }

    public override void Run()
    {
        BodyColor = Color.Green;
        while (IsRunning)
        {
            if (currentTarget != null && currentTarget.HasValue)
            {
                var target = currentTarget.Value;
                double angleToTarget = GetAngleTo(target.X, target.Y);
                TurnTo(angleToTarget); 
                Forward(100);
                Fire(1); 

                if (isHitBot)DriftAndFire();
                if (target.Energy <= 0)currentTarget = null; 
            }
            else ScanForEnemies();
            
        }
    }

    private BotInfoData? GetWeakestOpponent()
    {
        if (opponents.Count == 0) return null;

        BotInfoData? weakestOpponent = null;
        foreach (var opponent in opponents)
        {
            if (weakestOpponent == null || opponent.Energy < weakestOpponent.Value.Energy)
            {
                weakestOpponent = opponent;
            }
        }

        return weakestOpponent;
    }

    private void ScanForEnemies()
    {
        TurnRadarRight(30);
    }

    private double GetAngleTo(double targetX, double targetY)
    {
        double deltaX = targetX - X;
        double deltaY = targetY - Y;
        double angle = Math.Atan2(deltaY, deltaX) * 180 / Math.PI; 
        return NormalizeAngle(angle);
    }

    private void TurnTo(double angle)
    {
        double angleDifference = NormalizeAngle(angle - currentAngle); 
        TurnRadarRight(angleDifference);
        TurnRight(angleDifference);
        currentAngle = NormalizeAngle(currentAngle + angleDifference);
    }

    private double NormalizeAngle(double angle)
    {
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }

    private void DriftAndFire()
    {
        TurnLeft(90);
        Forward(50);
        TurnRight(90);
        Forward(50);
        Fire(1);
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        if (currentTarget == null || currentTarget.Value.Energy <= 0)
        {
            opponents.Add(new BotInfoData
            {
                X = e.X,
                Y = e.Y,
                Energy = e.Energy  
            });
            currentTarget = GetWeakestOpponent();
        }
    }

    public override void OnHitBot(HitBotEvent e)
    {
        isHitBot = true;
        DriftAndFire();
    }

    public override void OnHitWall(HitWallEvent e)
    {
        TurnLeft(180);
    }
}
