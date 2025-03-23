using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class Chh : Bot
{
    static void Main(string[] args)
    {
        new Chh().Start();
    }

    public enum BotState
    {
        EVADE,
        BATTLE,
        HIT,

    }

    BotState botState = BotState.EVADE;

    struct BotData
    {
        public int ID;
        public double lastEnergy;
        public double currentEnergy;
        public double X;
        public double Y;
        public int hitCount;
        public int shotCount;
    }
    private Dictionary<int, BotData> scannedBots = new();
    private int targetId = -1;
    private int stuckCooldown = 0;
    private int hitCooldown = 0;
    bool isMovingForward = true;

    Chh() : base(BotInfo.FromFile("Chh.json")) { }

    Random rand = new Random();

    public override void Run()
    {
        AdjustRadarForBodyTurn = false;
        AdjustGunForBodyTurn = false;
        AdjustRadarForGunTurn = false;

        TurretColor = Color.Black;
        ScanColor = Color.White;
        BulletColor = Color.White;
        BodyColor = Color.Black;
        RadarColor = Color.Black;
        TracksColor = Color.Black;
        GunColor = Color.Black;

        scannedBots.Clear();
        isMovingForward = true;
        stuckCooldown = 0;
        targetId = -1;

        while (IsRunning)
        {
            SetTurnRadarRight(45);
            if (stuckCooldown > 0) stuckCooldown--;
            if (hitCooldown > 0) hitCooldown--;
            bool isNearWall = (X < 30 || X > ArenaWidth - 30 || Y < 30 || Y > ArenaHeight - 30);
            // Console.WriteLine(botState);
            switch (botState)
            {
                case BotState.BATTLE:
                    if (isNearWall)
                    {
                        // Console.WriteLine("BALIK");
                        ReverseDirection(rand.Next(6, 24));
                    }
                    else
                    {
                        stuckCooldown = 0;
                    }


                    Rescan();
                    BotData botData;
                    if (scannedBots.TryGetValue(targetId, out botData))
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

    private void Dodge()
    {
        double minDistance = Double.PositiveInfinity;
        double gunAngle = 0.0;
        foreach (KeyValuePair<int, BotData> item in scannedBots)
        {
            int key = item.Key;
            BotData bd = item.Value;

            double dist = DistanceTo(bd.X, bd.Y);
            if (dist < minDistance)
            {
                minDistance = dist;
                gunAngle = GunBearingTo(bd.X, bd.Y);
            }
            double bearingTo = BearingTo(bd.X, bd.Y);

            if (bd.lastEnergy != -1 && bd.currentEnergy > 3 && bd.currentEnergy < bd.lastEnergy)
            {
                // Console.WriteLine("ID {0} : energy decrease {1} -> {2}", bd.ID, bd.lastEnergy, bd.currentEnergy);
                double rnd = Math.Sign(rand.NextDouble());
                var enemyBearing = Direction + bearingTo;

                SetTurnLeft(NormalizeRelativeAngle(enemyBearing + 120 * rnd - Direction));


                SetForward(rand.NextDouble() > 0.25 ? 100 : -100);

            }
            if (Math.Abs(gunAngle) < 30)
            {
                SmartFire(bd);
            }
        }
        // Console.WriteLine(minDistance);
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
        else if (Energy > bd.currentEnergy + 1 || rand.NextDouble() < hitProbability)
        {
            powerFire = 1;
        }

        if (Energy > powerFire + 1)
        {
            SetFire(powerFire);
            // Console.WriteLine($"{distance} => {powerFire}");
            // Console.WriteLine($"Hit probability: {hitP}");
        }
    }
    public override void OnBulletHit(BulletHitBotEvent evt)
    {
        if (scannedBots.TryGetValue(evt.VictimId, out var bdt))
        {
            bdt.hitCount++;
            bdt.lastEnergy = bdt.currentEnergy;
            bdt.currentEnergy = evt.Energy;
            scannedBots[bdt.ID] = bdt;
        }
    }
    public override void OnScannedBot(ScannedBotEvent evt)
    {

        var dist = DistanceTo(evt.X, evt.Y);
        if (hitCooldown == 0)
        {
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

        BotData bd;
        bd.ID = evt.ScannedBotId;
        bd.currentEnergy = evt.Energy;
        bd.X = evt.X;
        bd.Y = evt.Y;

        BotData lastBd;
        if (scannedBots.TryGetValue(bd.ID, out lastBd))
        {
            bd.lastEnergy = lastBd.currentEnergy;
            bd.hitCount = lastBd.hitCount;
            bd.shotCount = lastBd.shotCount;
        }
        else
        {
            bd.lastEnergy = -1;
            bd.hitCount = 0;
            bd.shotCount = 0;
        }
        scannedBots[evt.ScannedBotId] = bd;
        targetId = evt.ScannedBotId;

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
    public override void OnBulletFired(BulletFiredEvent evt)
    {
        BotData bdt;
        if (scannedBots.TryGetValue(targetId, out bdt))
        {
            bdt.shotCount++;
            scannedBots[bdt.ID] = bdt;
        }

    }

    public override void OnBotDeath(BotDeathEvent evt)
    {
        scannedBots.Remove(evt.VictimId);
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
