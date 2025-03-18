using System;
using System.Drawing;
using System.Drawing;
using System.Linq;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class PemburuAlatreon : Bot{   
    /* A bot that drives forward and backward, and fires a bullet */
    static void Main(string[] args){
        new PemburuAlatreon().Start();
    }

    PemburuAlatreon() : base(BotInfo.FromFile("PemburuAlatreon.json")) { }

    private class EnemyInfo{
        public int id;
        public double x;
        public double y;
        public double distance;
        public double lastScan;
    }

    private Dictionary<int, EnemyInfo> enemies = new Dictionary<int, EnemyInfo>();
    public override void Run(){
        AdjustGunForBodyTurn = true; // Gun bebas dengan pergerakan body
        AdjustRadarForBodyTurn = true; // Radar bebas dengan pergerakan body
        AdjustRadarForGunTurn = true; // Radar bebas dengan pergerakan gun

        /* Customize bot colors, read the documentation for more information */
        BodyColor = Color.Gray;

        while (IsRunning){
            SetTurnRadarLeft(360); 
            SetForward(100);
            SetTurnLeft(20);
            Go();

        }
    }

    public override void OnScannedBot(ScannedBotEvent e){
        double distance = DistanceTo(e.X, e.Y);
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

        // ====================== Gun Modifier ==================== //
        if (distance < 200){
            SetFire(3);
        }else if (distance < 500){
            SetFire(2);
        }else if (distance < 800){
            SetFire(1);
        }
        Go();
    }

    public override void OnHitBot(HitBotEvent e){
        SetTurnRight(90);
        SetBack(100);
        Go();
    }

    public virtual void OnHitByBullet(HitByBulletEvent e){
        SetTurnRight(-90);
        SetForward(100);
        Go();
    }

    public override void OnHitWall(HitWallEvent e){
        Console.WriteLine("Ouch! I hit a wall, must turn back!");
    }

    /* Read the documentation for more events and methods */

    /* Healper Method */

    private void UpdateEnemyInfo(ScannedBotEvent e){// Update info musuh
        double dist = DistanceTo(e.X, e.Y);

        if (!enemies.ContainsKey(e.ScannedBotID)){
            enemies[e.ScannedBotID] = new EnemyInfo{
                e.id = e.ScannedBotID,
                x = e.X,
                y = e.Y,
                distance = dist,
                lastScan = TurnNumber
            };
        }else {
            var enemy = enemies[e.ScannedBotID];
            enemy.x = e.X;
            enemy.y = e.Y;
            enemy.distance = dist;
            enemy.lastScan = TurnNumber;
        }
    }

    private EnemyInfo closestEnemy(){// Method untuk mengambil jarak terdekat bot musuh dengan player
        if (enemies.Count == 0){
            return null;
        }
        double maxTurn = 30;
        var validEnemy = enemies.Values.Where( => TurnNumber - e.lastScan <= maxTurn).ToList();
        if (validEnemy.Count == 0){
            return null;
        }
        var Closest = validEnemy.OrderBy(enemies => e.distance).FirstOrDefault();
        return Closest;
    }

    private void aimRadarandGun(EnemyInfo target){
        double gunbearing = GunBearingTo(target.x, target.y);

        if (gunbearing > 0){
            SetTurnGunLeft(gunbearing); // Memutar gun sejauh gunbearing 
        }else{
            SetTurnGunRight(-gunbearing); // Memutar gun sejauh gunbearing 
        }
        
        double radarbearing = RadarBearingTo(target.x, target.y); // Menghitung sudut antara radar dengan posisi bot musuh

        if (radarbearing > 0){
            SetTurnRadarLeft(radarbearing); // Memutar radar sejauh radarbearing 
        }else{
            SetTurnRadarRight(-radarbearing); // Memutar radar sejauh radarbearing
        }
    }

    private void distanceFireGun(double distance){
        double powerFire;
        if (distance < 200){
            powerFire = 3;
        }else if (distance < 400){
            powerFire = 2;
        }else{
            powerFire = 1
        }

        if (Energy > powerFire + 1){
            setFire(powerFire);
        }
    }
}
