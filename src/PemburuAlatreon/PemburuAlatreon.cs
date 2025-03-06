using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class PemburuAlatreon : Bot{   
    /* A bot that drives forward and backward, and fires a bullet */
    static void Main(string[] args){
        new PemburuAlatreon().Start();
    }

    PemburuAlatreon() : base(BotInfo.FromFile("PemburuAlatreon.json")) { }

    public override void Run(){
        /* Customize bot colors, read the documentation for more information */
        BodyColor = Color.Gray;

        while (IsRunning){
            SetTurnRadarLeft(360); 
            SetTurnGunLeft(360);
            Go();

        }
    }

    public override void OnScannedBot(ScannedBotEvent e){
        double turngun = normalizeAngle()
    }

    private double normalizeAngle(double angle){
        double normalize = angle % 360;
        if (normalize > 180){
            normalize -= 360;
        }else if (normalize <= -360){
            normalize += 360;
        }
        return normalize;
    }

    public override void OnHitBot(HitBotEvent e){
        Console.WriteLine("Ouch! I hit a bot at " + e.X + ", " + e.Y);
    }

    public override void OnHitWall(HitWallEvent e){
        Console.WriteLine("Ouch! I hit a wall, must turn back!");
    }

    /* Read the documentation for more events and methods */
}
