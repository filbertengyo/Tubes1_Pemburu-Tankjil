using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class Bhh : Bot
{
    static void Main(string[] args)
    {
        new Bhh().Start();
    }

    private bool foundBot = false;
    private int TurnDir = 1;

    Bhh() : base(BotInfo.FromFile("Bhh.json")) { }

    public override void OnRoundStarted(RoundStartedEvent evt)
    {
        foundBot = false;
    }
    public override void Run()
    {

        AdjustRadarForBodyTurn = false;
        AdjustGunForBodyTurn = false;
        AdjustRadarForGunTurn = false;

        ScanColor = Color.FromArgb(0xFF, 0x8C, 0x00);
        TurretColor = Color.FromArgb(0xFF, 0xA5, 0x00);
        RadarColor = Color.FromArgb(0xFF, 0xD7, 0x00);
        BulletColor = Color.FromArgb(0xFF, 0x45, 0x00);
        BodyColor = Color.FromArgb(0xFF, 0xFF, 0x00);
        TracksColor = Color.FromArgb(0x99, 0x33, 0x00);
        GunColor = Color.FromArgb(0xCC, 0x55, 0x00);

        while (IsRunning)
        {
            if (!foundBot)
            {
                WaitFor(new RadarTurnCompleteCondition(this));
                SetTurnRadarRight(360 * TurnDir);
            }
            else
            {
                foundBot = false;
                Rescan();
            }

            Go();
        }
    }

    private void SmartFire(double distance)
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


    public override void OnScannedBot(ScannedBotEvent evt)
    {
        // RAMMMMMM

        var bearing = BearingTo(evt.X, evt.Y);
        var gunBearing = GunBearingTo(evt.X, evt.Y);
        var distance = DistanceTo(evt.X, evt.Y);
        var speed = Math.Abs(bearing) < 20 ? 50 : 20;

        SetForward(speed);
        SetTurnLeft(bearing);
        SetTurnGunLeft(gunBearing);
        SetTurnRadarRight(0);
        SetTurnRadarLeft(RadarTurnRemaining * TurnDir);

        TurnDir *= -1;

        // SHOOOOOOT
        if (gunBearing < 10)
        {
            SmartFire(distance);
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

    public override void OnHitWall(HitWallEvent e)
    {
        TurnRight(180);
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
