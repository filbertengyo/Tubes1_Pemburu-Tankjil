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
        double distance = e.GetDistance();
        // ====================== Lock On==================== //
        double gunbearing = GunBearingTo(e.X, e.Y); // Menghitung sudut antara gun dengan posisi bot musuh

        if (gunbearing > 0){
            SetTurnGunLeft(gunbearing); // Memutar gun sejauh gunbearing 
        }else{
            SetTurnGunRight(-gunbearing); // Memutar gun sejauh gunbearing 
        }
        
        double radarbearing = RadarBearingTo(e.X, e.Y); // Menghitung sudut antara radar dengan posisi bot musuh

        if (radarbearing > 0){
            SetTurnRadarLeft(radarbearing); // Memutar radar sejauh radarbearing 
        }else{
            SetTurnRadarRight(-radarbearing); // Memutar radar sejauh radarbearing
        }

        // ====================================================== //

        if (distance)
        SetFire(1);
        Go();
    }

    public override void OnHitBot(HitBotEvent e){
        Console.WriteLine("Ouch! I hit a bot at " + e.X + ", " + e.Y);
    }

    public override void OnHitWall(HitWallEvent e){
        Console.WriteLine("Ouch! I hit a wall, must turn back!");
    }

    /* Read the documentation for more events and methods */
}
