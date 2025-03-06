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
        AdjustGunForBodyTurn = true; // Gun bebas dengan pergerakan body
        AdjustRadarForBodyTurn = true; // Radar bebas dengan pergerakan body
        AdjustRadarForGunTurn = true; // Radar bebas dengan pergerakan gun

        /* Customize bot colors, read the documentation for more information */
        BodyColor = Color.Gray;

        while (IsRunning){
            SetForward(100);
            SetTurnLeft(20);
            SetTurnRadarLeft(360); 
            Go();

        }
    }

    public override void OnScannedBot(ScannedBotEvent e){
        // ====================== Lock On==================== //
        double gunbearing = GunBearingTo(e.X, e.Y);

        if (gunbearing > 0){
            SetTurnGunLeft(gunbearing);
        }else{
            SetTurnGunRight(-gunbearing);
        }
        
        double radarbearing = RadarBearingTo(e.X, e.Y);

        if (radarbearing > 0){
            SetTurnRadarLeft(radarbearing);
        }else{
            SetTurnRadarRight(-radarbearing);
        }

        SetFire(1);
        // ====================================================== //
        Go();
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
