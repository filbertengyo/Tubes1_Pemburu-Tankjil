using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class Bhh : Bot
{
    // The main method starts our bot
    static void Main(string[] args)
    {
        new Bhh().Start();
    }

    public class ScannedBot
    {
        public double X;
        public double Y;
        public ScannedBot(ScannedBotEvent bot)
        {
            this.X = bot.X;
            this.Y = bot.Y;

        }
    }

    private bool foundBot;
    private List<ScannedBot> scannedBots = new();
    private ScannedBot scannedBot;
    private int TurnDir = 1;

    // Constructor, which loads the bot config file
    Bhh() : base(BotInfo.FromFile("Bhh.json")) { }

    // Called when a new round is started -> initialize and do some movement
    public override void Run()
    {
        foundBot = false;
        AdjustRadarForBodyTurn = false;
        AdjustGunForBodyTurn = false;
        AdjustRadarForGunTurn = false;

        ScanColor = Color.FromArgb(0xFF, 0x8C, 0x00);   // Dark Orange
        TurretColor = Color.FromArgb(0xFF, 0xA5, 0x00); // Orange
        RadarColor = Color.FromArgb(0xFF, 0xD7, 0x00);  // Gold
        BulletColor = Color.FromArgb(0xFF, 0x45, 0x00); // Orange-Red
        BodyColor = Color.FromArgb(0xFF, 0xFF, 0x00);   // Bright Yellow
        TracksColor = Color.FromArgb(0x99, 0x33, 0x00); // DarkBrownish - Orange
        GunColor = Color.FromArgb(0xCC, 0x55, 0x00);    // Medium Orange

        while (IsRunning)
        {
            // Console.WriteLine(foundBot);
            if (!foundBot)
            {
                WaitFor(new RadarTurnCompleteCondition(this));
                SetTurnRadarRight(360 * TurnDir);
            }
            else
            {
                foundBot = false;
                // ScannedBot evt = scannedBot;
                // var bearing = BearingTo(evt.X, evt.Y);
                // var gunBearing = GunBearingTo(evt.X, evt.Y);
                // Console.WriteLine(bearing);
                // var distance = DistanceTo(evt.X, evt.Y);
                // var speed = Math.Abs(bearing) < 20 ? 50 : 20;
                // SetForward(speed);
                // SetTurnLeft(bearing);
                // SetTurnGunLeft(gunBearing);
                // SetTurnRadarRight(0);
                // SetTurnRadarLeft(RadarTurnRemaining * TurnDir);
                // TurnDir *= -1;
                // if (distance < 300)
                // {
                //     SetFire(1);
                // }
                Rescan();
            }

            Go();
        }
    }

    private void distanceFireGun(double distance)
    {
        double powerFire;
        if (distance < 200)
        {
            powerFire = 3;
        }
        else if (distance < 400)
        {
            powerFire = 2;
        }
        else
        {
            powerFire = 1;
        }

        if (Energy > powerFire + 1)
        {
            SetFire(powerFire);
        }
    }


    // We saw another bot -> fire!
    public override void OnScannedBot(ScannedBotEvent evt)
    {
        // scannedBots.Add(new ScannedBot(evt));
        // scannedBot = new ScannedBot(evt);
        var bearing = BearingTo(evt.X, evt.Y);
        var gunBearing = GunBearingTo(evt.X, evt.Y);
        // Console.WriteLine(bearing);
        var distance = DistanceTo(evt.X, evt.Y);
        var speed = Math.Abs(bearing) < 20 ? 50 : 20;
        SetForward(speed);
        SetTurnLeft(bearing);
        SetTurnGunLeft(gunBearing);
        SetTurnRadarRight(0);
        SetTurnRadarLeft(RadarTurnRemaining * TurnDir);
        TurnDir *= -1;
        if (gunBearing < 10)
        {
            distanceFireGun(distance);
            // SetFire(1);
        }
        Go();
        foundBot = true;
    }

    public override void OnHitBot(HitBotEvent botHitBotEvent)
    {
        TurnRight(90);
        SetForward(0);
        SetBack(50);
        Go();

    }

    // We collided with a wall -> reverse the direction
    public override void OnHitWall(HitWallEvent e)
    {
        TurnRight(180);
        // if (movingForward)
        // {
        //     SetBack(40000);
        //     movingForward = false;
        // }
        // else
        // {
        //     SetForward(40000);
        //     movingForward = true;
        // }
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
