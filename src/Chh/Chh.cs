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

    private int centerX;
    private int centerY;

    const int NEAR_WALL_OFFSET = 60;

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

        centerX = ArenaWidth / 2;
        centerY = ArenaHeight / 2;

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

            bool isNearWall = (X < NEAR_WALL_OFFSET || X > ArenaWidth - NEAR_WALL_OFFSET || Y < NEAR_WALL_OFFSET || Y > ArenaHeight - NEAR_WALL_OFFSET);

            SetTurnRadarRight(45);
            switch (botState)
            {
                case BotState.BATTLE:

                    if (isNearWall)
                    {
                        // Console.WriteLine("DEKET");
                        if (stuckCooldown > 8)
                        {
                            Move(centerX, centerY);
                        }
                        else
                        {
                            ReverseDirection(rand.Next(6, 12));
                        }
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

                    Move(centerX, centerY);

                    if (!isNearWall)
                    {
                        GreedyEvade();
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
    private void GreedyEvade()
    {
        double minDistance = Double.PositiveInfinity;
        double maxProbability = -1.0;
        double gunAngle = 0.0;
        BotData nearestBot = null;
        BotData probableTarget = null;

        double firePower = 0;
        foreach (KeyValuePair<int, BotData> item in scannedBots)
        {
            BotData bd = item.Value;

            double dist = DistanceTo(bd.X, bd.Y);

            // // Store the closest bot data
            if (dist < minDistance)
            {
                minDistance = dist;
                // gunAngle = GunBearingTo(bd.X, bd.Y);
                nearestBot = bd;
            }

            double hitProbability;

            firePower = CalculatePowerFire(bd, out hitProbability);

            // Console.WriteLine($"P{bd.ID}: {Math.Round(hitProbability, 4)}");
            if (hitProbability > maxProbability)
            {
                maxProbability = hitProbability;
                probableTarget = bd;
                gunAngle = GunBearingTo(bd.X, bd.Y);
                // Console.WriteLine($"Highest P{bd.ID}: {Math.Round(hitProbability, 4)}");
            }

        }

        if (probableTarget != null && Math.Abs(gunAngle) < 12)
        {
            SmartFire(probableTarget, firePower);
        }

        Dodge(nearestBot);

        SetTurnGunLeft(gunAngle);
    }

    private void Dodge(BotData nearestBot)
    {
        if (nearestBot != null && nearestBot.lastEnergy != -1 && nearestBot.currentEnergy > 3 && nearestBot.currentEnergy < nearestBot.lastEnergy)
        {
            double bearingTo = BearingTo(nearestBot.X, nearestBot.Y);
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
        if (Math.Abs(angle) < 90)
        {
            SetForward(50);
        }
        else
        {
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

    private double CalculatePowerFire(BotData bd, out double hitProbability)
    {
        double distance = DistanceTo(bd.X, bd.Y);
        hitProbability = bd.shotCount > 0 ? (double)bd.hitCount / bd.shotCount : 1;
        double distanceFactor = (1.0 - hitProbability) * 200;

        double powerFire = 0;
        // if ((distance < 200 - distanceFactor) || rand.NextDouble() * 3 < hitProbability)
        if (distance < 200 - distanceFactor)
        {
            powerFire = 3;
        }
        // else if ((distance < 400 - distanceFactor) || rand.NextDouble() * 2 < hitProbability)
        else if (distance < 400 - distanceFactor)
        {
            powerFire = 2;
        }
        else if ((distance < 800 && Energy > bd.currentEnergy + 1))//|| rand.NextDouble() < hitProbability)
        {
            powerFire = 1;
        }

        hitProbability = (hitProbability * powerFire) / (distance * distance);

        return powerFire;

    }

    private void SmartFire(BotData bd, double firePower = -1.0)
    {
        if (firePower < 0)
        {
            firePower = CalculatePowerFire(bd, out _);
        }

        if (Energy > firePower + 1)
        {
            SetFire(firePower);
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

    public override void OnSkippedTurn(SkippedTurnEvent evt)
    {
        Console.WriteLine("SKIPPED");
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
