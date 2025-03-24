using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class DrinkAndDrive : Bot
{
    public enum BotState
    {
        MOVEMENT,
        BATTLE,
        ESCAPE,
    }

    BotState botState = BotState.MOVEMENT;

    Random rand = new Random();
    private Dictionary<int, BotData> scannedBots = new();
    private int targetId = -1;
    private int stuckCooldown = 0;
    private int hitCooldown = 0;
    private bool isMovingForward = true;

    private class BotData
    {
        public int ID;
        public int hitCount;
        public int shotCount;
        public double lastEnergy;
        public double currentEnergy;
        public double X;
        public double Y;

        public BotData(){}

        public BotData(int ID, double lastEnergy, double currentEnergy, double X, double Y, int hitCount, int shotCount)
        {
            this.ID = ID;
            this.lastEnergy = lastEnergy;
            this.currentEnergy = currentEnergy;
            this.X = X;
            this.Y = Y;
            this.hitCount = hitCount;
            this.shotCount = shotCount;
        }
    }

    private int centerX;
    private int centerY;

    const int NEAR_WALL_OFFSET = 60;

    DrinkAndDrive() : base(BotInfo.FromFile("DrinkAndDrive.json")) { }

    static void Main(string[] args)
    {
        new DrinkAndDrive().Start();
    }

    public override void OnRoundStarted(RoundStartedEvent evt)
    {
        scannedBots.Clear();
        isMovingForward = true;
        stuckCooldown = 0;
        hitCooldown = 0;
        targetId = -1;
    }

    public override void Run()
    {
        AdjustRadarForBodyTurn = false;
        AdjustGunForBodyTurn = false;
        AdjustRadarForGunTurn = false;

        centerX = ArenaWidth / 2;
        centerY = ArenaHeight / 2;

        BodyColor = Color.FromArgb(20, 61, 96);
        TurretColor = Color.FromArgb(235, 91, 0);
        RadarColor = Color.FromArgb(38, 31, 79);
        BulletColor = Color.FromArgb(235, 91, 0);
        ScanColor = Color.FromArgb(229, 32, 32);
        TracksColor = Color.FromArgb(25, 91, 0);
        GunColor = Color.FromArgb(20, 61, 96);

        while (IsRunning)
        {
            if (stuckCooldown > 0) stuckCooldown--;
            if (hitCooldown > 0) hitCooldown--;

            bool isNearWall = (X < NEAR_WALL_OFFSET || X > ArenaWidth - NEAR_WALL_OFFSET || Y < NEAR_WALL_OFFSET || Y > ArenaHeight - NEAR_WALL_OFFSET);

            SetTurnRadarRight(45);
            switch (botState)
            {
                case BotState.BATTLE:

                    if (isNearWall)
                    {
                        if (stuckCooldown > 8) Move(centerX, centerY);
                        else RunAway(rand.Next(6, 12));
                    }
                    else stuckCooldown = 0;


                    Rescan();

                    if (scannedBots.TryGetValue(targetId, out BotData botData))
                    {
                        SmartFire(botData);
                    }

                    if (isMovingForward)
                    {
                        SetBack(0);
                        SetForward(50);
                    }
                    else
                    {
                        SetForward(0);
                        SetBack(50);
                    }
                    break;

                case BotState.MOVEMENT:

                    Move(centerX, centerY);

                    if (!isNearWall) LockTarget();
                    else stuckCooldown = 0;
                    
                    break;

                case BotState.ESCAPE:
                    SetTurnRight(45);
                    SetForward(0);
                    SetBack(300);
                    break;

                default:
                    break;
            }
            Go();
        }
    }

    private void LockTarget()
    {
        double minEnergy = Double.PositiveInfinity;
        BotData lowestHPBot = null;

        foreach (KeyValuePair<int, BotData> item in scannedBots)
        {
            BotData bd = item.Value;

            if (bd.currentEnergy < minEnergy)
            {
                minEnergy = bd.currentEnergy;
                lowestHPBot = bd;
            }
        }

        if (lowestHPBot != null)
        {
            targetId = lowestHPBot.ID;
            Dodge(lowestHPBot);
        }
    }

    private void Dodge(BotData lowestHPBot)
    {
        if (lowestHPBot != null && lowestHPBot.lastEnergy != -1 && lowestHPBot.currentEnergy > 3 && lowestHPBot.currentEnergy < lowestHPBot.lastEnergy)
        {
            double bearingTo = BearingTo(lowestHPBot.X, lowestHPBot.Y);
            var enemyBearing = Direction + bearingTo;

            double rnd = Math.Sign(rand.NextDouble());
            SetTurnLeft(NormalizeRelativeAngle(enemyBearing + 90 * rnd - Direction));

            SetForward(rand.NextDouble() > 0.25 ? 100 : -100);
        }
    }

    private void Move(double X, double Y)
    {
        var angle = BearingTo(X, Y);
        SetTurnLeft(angle);
        if (Math.Abs(angle) < 90) SetForward(50);
        else SetBack(50);
    }

    private void RunAway(int duration = 4)
    {
        if (stuckCooldown == 0)
        {
            isMovingForward = !isMovingForward;
            stuckCooldown = duration;
        }
    }

    private double CalculatePowerFire(BotData bd, out double hitProbability)
    {
        double distance = DistanceTo(bd.X, bd.Y);
        hitProbability = bd.shotCount > 0 ? (double)bd.hitCount / bd.shotCount : 1;
        double distanceFactor = (1.0 - hitProbability) * 200;

        double powerFire = 0;
        if (distance < 200 - distanceFactor) powerFire = 3;
        else if (distance < 400 - distanceFactor) powerFire = 2;
        else if ((distance < 800 && Energy > bd.currentEnergy + 1))powerFire = 1;

        hitProbability = (hitProbability * powerFire) / (distance * distance);
        return powerFire;
    }

    private void SmartFire(BotData bd, double firePower = -1.0)
    {
        if (firePower < 0) firePower = CalculatePowerFire(bd, out _);
        if (Energy > firePower + 1) SetFire(firePower);
    }

    private void RotateRadar(double x, double y)
    {
        double radarbearing = RadarBearingTo(x, y);
        double margin = 5;

        if (radarbearing > 0) SetTurnRadarLeft(radarbearing + margin);
        else SetTurnRadarRight(-radarbearing + margin);
    }

    public override void OnBulletHit(BulletHitBotEvent evt)
    {
        if (scannedBots.TryGetValue(evt.VictimId, out var bdt))
        {
            bdt.hitCount++;
            bdt.lastEnergy = bdt.currentEnergy;
            bdt.currentEnergy = evt.Energy;
        }
    }

    public override void OnBulletFired(BulletFiredEvent evt)
    {
        if (scannedBots.TryGetValue(targetId, out var bd))
        {
            bd.shotCount++;
        }
    }

    public override void OnBotDeath(BotDeathEvent evt)
    {
        scannedBots.Remove(evt.VictimId);
    }

    public override void OnScannedBot(ScannedBotEvent evt)
    {
        if (hitCooldown == 0)
        {
            var dist = DistanceTo(evt.X, evt.Y);

            if (dist < 200 || EnemyCount == 1)
            {
                botState = BotState.BATTLE;

                var bearing = BearingTo(evt.X, evt.Y);
                var gunBearing = GunBearingTo(evt.X, evt.Y);

                SetTurnLeft(bearing + 90);
                SetTurnGunLeft(gunBearing);

                RotateRadar(evt.X, evt.Y);
            }
            else 
            {
                SetTurnRight(0);
                botState = BotState.MOVEMENT;
            }
        }

        if (scannedBots.TryGetValue(evt.ScannedBotId, out BotData bd))
        {
            bd.X = evt.X;
            bd.Y = evt.Y;
            bd.lastEnergy = bd.currentEnergy;
            bd.currentEnergy = evt.Energy;
        }
        else
        {
            BotData newBd = new BotData(evt.ScannedBotId, -1, evt.Energy, evt.X, evt.Y, 0, 0);
            scannedBots[evt.ScannedBotId] = newBd;
        }

        targetId = evt.ScannedBotId;
    }

    public override void OnHitBot(HitBotEvent evt)
    {
        if (evt.VictimId != targetId && EnemyCount > 1)
        {
            botState = BotState.ESCAPE;
            hitCooldown = 6;
            RotateRadar(evt.X, evt.Y);
        }
    }

    public override void OnHitWall(HitWallEvent e)
    {
        RunAway();
    }
}
