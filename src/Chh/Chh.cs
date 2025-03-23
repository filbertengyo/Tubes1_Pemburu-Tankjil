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
        RUN,
        HUNT

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
            if (stuckCooldown > 0) stuckCooldown--;
            // Console.WriteLine(botState);
            switch (botState)
            {
                case BotState.RUN:
                    if (X < 30 || X > ArenaWidth - 30 || Y < 30 || Y > ArenaHeight - 30)
                    {
                        ReverseDirection(rand.Next(2, 10));
                    }

                    SetTurnRadarRight(45);
                    
                    Rescan();
                    // SetTurnGunLeft(GunBearingTo(targetBot.X, targetBot.Y));
                    // SetTurnLeft(CalcDeltaAngle(Direction, targetBot.Direction + 90));
                    BotData botData;
                    if (scannedBots.TryGetValue(targetId, out botData))
                    {
                        SmartFire(botData);
                    }
                    if (isMovingForward)
                    {
                        SetForward(50);
                    }
                    else
                    {

                        SetBack(50);
                    }
                    break;

                case BotState.EVADE:

                    SetTurnRadarRight(45);
                    if (X < 30 || X > ArenaWidth - 30 || Y < 30 || Y > ArenaHeight - 30)
                    {
                        // Console.WriteLine($"{Math.Round(X)}/{ArenaWidth}, {Math.Round(Y)}/{ArenaHeight}");
                        // SetTurnLeft(BearingTo(ArenaWidth / 2, ArenaHeight / 2));
                        // SetForward(10);
                        // SetBack(0);
                        Move(ArenaWidth / 2, ArenaHeight / 2);
                    }
                    else
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
                                
                                SetTurnRight(NormalizeRelativeAngle(enemyBearing + 90 * rnd - Direction));

                                
                                var amount = 500 / dist;
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

                    break;

                default:

                    break;
            }
            Go();
        }
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
            // Console.WriteLine("STUCK");
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
        else if ((EnemyCount > 1 && Energy > bd.currentEnergy + 1) || rand.NextDouble() >= hitProbability)
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
            scannedBots[bdt.ID] = bdt;
        }
    }
    public override void OnScannedBot(ScannedBotEvent evt)
    {

        var dist = DistanceTo(evt.X, evt.Y);
        if (dist < 200 || EnemyCount == 1)
        {
            botState = BotState.RUN;
            var bearing = BearingTo(evt.X, evt.Y);
            var gunBearing = GunBearingTo(evt.X, evt.Y);

            SetTurnLeft(bearing + 90);
            SetTurnGunLeft(gunBearing);
            double radarbearing = RadarBearingTo(evt.X, evt.Y);
            double margin = 5;
            if (radarbearing > 0)
            {
                SetTurnRadarLeft(radarbearing + margin);
            }
            else
            {
                SetTurnRadarRight(-radarbearing + margin);
            }
            Go();

        }
        else
        {
            SetTurnRight(0);
            botState = BotState.EVADE;
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
    public override void OnBulletFired(BulletFiredEvent evt)
    {
        BotData bdt;
        if (scannedBots.TryGetValue(targetId, out bdt))
        {
            bdt.shotCount++;
            scannedBots[bdt.ID] = bdt;
        }

    }

    public override void OnBotDeath(BotDeathEvent botDeathEvent)
    {
        scannedBots.Remove(botDeathEvent.VictimId);
    }
    public override void OnHitBot(HitBotEvent botHitBotEvent)
    {
    }

    public override void OnHitWall(HitWallEvent e)
    {
        ReverseDirection();
    }
    public class TurnCompleteCondition : Condition
    {
        private readonly Bot bot;

        public TurnCompleteCondition(Bot bot)
        {
            this.bot = bot;
        }

        public override bool Test()
        {
            return bot.TurnRemaining == 0;
        }
    }
    public class RadarTurnCompleteCondition : Condition
    {
        private readonly Bot bot;

        public RadarTurnCompleteCondition(Bot bot)
        {
            this.bot = bot;
        }

        public override bool Test()
        {
            return bot.RadarTurnRemaining == 0;
        }
    }
}
