using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class Chh : Bot
{

    public enum BotState
    {
        EVADE,
        BATTLE,
        HIT,

    }

    BotState botState = BotState.EVADE;

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

        public BotData()
        {

        }
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

    struct sBotData
    {
        public int ID;
        public double lastEnergy;
        public double currentEnergy;
        public double X;
        public double Y;
        public int hitCount;
        public int shotCount;
    }

    Chh() : base(BotInfo.FromFile("Chh.json")) { }

    static void Main(string[] args)
    {
        new Chh().Start();
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
        // Set properties
        AdjustRadarForBodyTurn = false;
        AdjustGunForBodyTurn = false;
        AdjustRadarForGunTurn = false;

        // Set colors
        TurretColor = Color.Black;
        ScanColor = Color.White;
        BulletColor = Color.White;
        BodyColor = Color.Black;
        RadarColor = Color.Black;
        TracksColor = Color.Black;
        GunColor = Color.Black;

        // Main loop
        while (IsRunning)
        {
            // Decrement cooldowns
            if (stuckCooldown > 0) stuckCooldown--;
            if (hitCooldown > 0) hitCooldown--;

            bool isNearWall = (X < 30 || X > ArenaWidth - 30 || Y < 30 || Y > ArenaHeight - 30);

            SetTurnRadarRight(45);
            switch (botState)
            {
                case BotState.BATTLE:

                    if (isNearWall)
                    {
                        ReverseDirection(rand.Next(6, 24));
                    }
                    else
                    {
                        stuckCooldown = 0;
                    }

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

                case BotState.EVADE:

                    Move(ArenaWidth / 2, ArenaHeight / 2);

                    if (!isNearWall)
                    {
                        Dodge();
                    }
                    else
                    {
                        stuckCooldown = 0;
                    }

                    break;

                case BotState.HIT:
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

    /*
     * ACTIONS
     */
    private void Dodge()
    {
        double minDistance = Double.PositiveInfinity;
        double gunAngle = 0.0;
        BotData nearestTarget = null;

        foreach (KeyValuePair<int, BotData> item in scannedBots)
        {
            BotData bd = item.Value;

            double dist = DistanceTo(bd.X, bd.Y);

            // Store the closest bot data
            if (dist < minDistance)
            {
                minDistance = dist;
                gunAngle = GunBearingTo(bd.X, bd.Y);
                nearestTarget = bd;
            }

            // Detect enemy's "shot" and dodge accordingly
            if (bd.lastEnergy != -1 && bd.currentEnergy > 3 && bd.currentEnergy < bd.lastEnergy)
            {
                double bearingTo = BearingTo(bd.X, bd.Y);
                var enemyBearing = Direction + bearingTo;

                double rnd = Math.Sign(rand.NextDouble());
                SetTurnLeft(NormalizeRelativeAngle(enemyBearing + 120 * rnd - Direction));

                SetForward(rand.NextDouble() > 0.25 ? 100 : -100);
            }
        }

        if (nearestTarget != null && Math.Abs(gunAngle) < 12)
        {
            SmartFire(nearestTarget);
        }

        SetTurnGunLeft(gunAngle);
    }

    private void Move(double X, double Y)
    {
        var angle = BearingTo(X, Y);

        if (Math.Abs(angle) < 90)
        {
            SetTurnLeft(angle);
            SetForward(50);
        }
        else
        {
            SetTurnLeft(NormalizeRelativeAngle(180 - angle));
            SetBack(50);
        }

    }

    private void ReverseDirection(int duration = 4)
    {
        if (stuckCooldown == 0)
        {
            isMovingForward = !isMovingForward;
            stuckCooldown = duration;
        }

    }

    private void SmartFire(BotData bd)
    {
        double distance = DistanceTo(bd.X, bd.Y);
        double powerFire = 0;
        double hitProbability = bd.shotCount > 0 ? (double)bd.hitCount / bd.shotCount : 1;
        double distanceFactor = (1.0 - hitProbability) * 200;

        if (distance < 200 - distanceFactor)
        {
            powerFire = 3;
        }
        else if (distance < 400 - distanceFactor)
        {
            powerFire = 2;
        }
        else if ((distance < 800 && Energy > bd.currentEnergy + 1) || rand.NextDouble() < hitProbability)
        {
            powerFire = 1;
        }

        if (Energy > powerFire + 1)
        {
            SetFire(powerFire);
        }
    }
    private void RotateRadar(double x, double y)
    {
        double radarbearing = RadarBearingTo(x, y);
        double margin = 5;

        if (radarbearing > 0)
        {
            SetTurnRadarLeft(radarbearing + margin);
        }
        else
        {
            SetTurnRadarRight(-radarbearing + margin);
        }
    }

    /*
     * EVENT HANDLING
     */

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
                botState = BotState.EVADE;
            }

        }

        // Update bot data
        // or create new bot data
        if (scannedBots.TryGetValue(evt.ScannedBotId, out BotData bd))
        {
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

    public override void OnSkippedTurn(SkippedTurnEvent evt)
    {
        // Console.WriteLine("SKIPPED");
    }

    public override void OnHitBot(HitBotEvent evt)
    {
        if (evt.VictimId != targetId && EnemyCount > 1)
        {
            botState = BotState.HIT;
            hitCooldown = 6;
            RotateRadar(evt.X, evt.Y);
        }
    }

    public override void OnHitWall(HitWallEvent e)
    {
        ReverseDirection();
    }

}
