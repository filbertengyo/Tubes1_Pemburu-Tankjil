using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
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
        public double energy;
    }
    // Dictionary untuk menyimpan informasi bot musuh
    private Dictionary<int, EnemyInfo> enemies = new Dictionary<int, EnemyInfo>();
    private Random randomMovement = new Random();
    private int movementDirection = 1;

    private Dictionary<int, double> lastEnergyMap = new Dictionary<int, double>();

    private int lastRadarScanTurn = 0;

    public override void Run(){
        AdjustGunForBodyTurn = true; // Gun bebas dengan pergerakan body
        AdjustRadarForBodyTurn = true; // Radar bebas dengan pergerakan body
        AdjustRadarForGunTurn = true; // Radar bebas dengan pergerakan gun

        /* Customize bot colors, read the documentation for more information */
        BodyColor = Color.Gray;
        BulletColor = Color.Red;
        TurretColor = Color.Blue;
        GunColor = Color.Blue;

        while (IsRunning){
            var target = closestEnemy();

            if (target != null){// jika lagi target ke musuh
                double enemyAngle = DirectionTo(target.x, target.y);
                double distToEnemy = DistanceTo(target.x, target.y);
                // Jika didapatkan jarak target ke bot < 60 diasumsikan bot target mendekat
                // Sehingga bot alatreon akan menghindar dari bot target
                if (distToEnemy < 60){
                    double dodgeAngle = normalizeAbsoluteAngle(enemyAngle + (randomMovement.Next(90, 181) * movementDirection));
                    double turnAngle = normalizeRelativeAngle(dodgeAngle - Direction);

                    if (turnAngle > 0){
                        SetTurnLeft(turnAngle);
                    }else{
                        SetTurnRight(-turnAngle);
                    }
                    SetBack(200);
                }else{
                    double forwardDistance = 50;
                    if (target.distance < 150){
                        forwardDistance = -150;
                    }else if (target.distance < 400){
                        forwardDistance = 150;
                    }else{
                        forwardDistance = 50;
                    }

                    double headingTo = normalizeAbsoluteAngle(enemyAngle + (90 * movementDirection));
                    double turnAngle = normalizeRelativeAngle(headingTo - Direction);

                    if (turnAngle > 0){
                        SetTurnLeft(turnAngle);
                    }else{
                        SetTurnRight(-turnAngle);
                    }

                    if (forwardDistance > 0){
                        SetForward(forwardDistance);
                    }else{
                        SetBack(-forwardDistance);
                    }
                }
                
            }else{// jika tidak ada musuh
                int randomTurn = randomMovement.Next(-30, 31);
                if (randomTurn > 0){
                    SetTurnLeft(randomTurn);
                }else{
                    SetTurnRight(-randomTurn);
                }
                SetForward(50);
            }
            if (TurnNumber  - lastRadarScanTurn > 2){
                SetTurnRadarLeft(360); 
            }else{
                SetTurnRadarLeft(360); 
            }
            
            Go();
        }
    }

    public override void OnScannedBot(ScannedBotEvent e){
        lastRadarScanTurn = TurnNumber;
        UpdateEnemyInfo(e);

        var targetBot = closestEnemy();
        if (targetBot == null){
            return;
        }

        aimRadarandGun(targetBot);
        // Melacak Energi terakhir dan Energi sekarang musuh.
        // Jika terdapat pengurangan lebih dari 0.1 diasumsikan musuh menembak
        // Sehingga bot akan melakukan dodge
        if (lastEnergyMap.TryGetValue(e.ScannedBotId, out double oldEnergy)){
            if (oldEnergy - e.Energy > 0.1){
                double dodgeAngle = randomMovement.Next(-60, 61);
                if (dodgeAngle > 0){
                    SetTurnLeft(dodgeAngle);
                }else{
                    SetTurnRight(-dodgeAngle);
                }
                SetForward(80);
            }
        }
        lastEnergyMap[e.ScannedBotId] = e.Energy;
        // Tembak jika bearing kecil
        double gunbearing = GunBearingTo(targetBot.x, targetBot.y);
        if (Math.Abs(gunbearing) < 5){
            distanceFireGun(targetBot.distance);

        }
        
    }

    public override void OnHitBot(HitBotEvent e){
        double randomAngle = randomMovement.Next(90, 181);
        TurnRight(randomAngle);
        Back(150);
    }

    public virtual void OnHitByBullet(HitByBulletEvent e){
        SetTurnRight(45);
        SetForward(150);
        Go();
    }

    public override void OnHitWall(HitWallEvent e){
        movementDirection *= -1;
        TurnRight(90);
        Back(100);
        
    }

    /* Read the documentation for more events and methods */

    /* Healper Method */

    private void UpdateEnemyInfo(ScannedBotEvent e){// Update info musuh
        double dist = DistanceTo(e.X, e.Y);

        if (!enemies.ContainsKey(e.ScannedBotId)){
            enemies[e.ScannedBotId] = new EnemyInfo{
                id = e.ScannedBotId,
                x = e.X,
                y = e.Y,
                distance = dist,
                lastScan = TurnNumber,
                energy = e.Energy
            };
        }else {
            var enemy = enemies[e.ScannedBotId];
            enemy.x = e.X;
            enemy.y = e.Y;
            enemy.distance = dist;
            enemy.lastScan = TurnNumber;
            enemy.energy = e.Energy;
        }
    }

    private EnemyInfo closestEnemy(){// Method untuk mengambil jarak terdekat bot musuh dengan player
        if (enemies.Count == 0){
            return null;
        }
        double maxTurn = 30;
        // mengambil enemy info yang valid dan mengonversi data tersebut menjadi list
        var validEnemy = enemies.Values.Where(enemy => TurnNumber - enemy.lastScan <= maxTurn).ToList(); 
        if (validEnemy.Count == 0){
            return null;
        }
        var Closest = validEnemy.OrderBy(enemy => enemy.distance).FirstOrDefault(); // mengurutkan musuh yang terdekat
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
        double margin = 5;
        if (radarbearing > 0){
            SetTurnRadarLeft(radarbearing + 5); // Memutar radar sejauh radarbearing 
        }else{
            SetTurnRadarRight(-radarbearing + 5); // Memutar radar sejauh radarbearing
        }
    }

    private void distanceFireGun(double distance){
        double powerFire;
        if (distance < 200){
            powerFire = 3;
        }else if (distance < 400){
            powerFire = 2;
        }else{
            powerFire = 1;
        }

        if (Energy > powerFire + 1){
            SetFire(powerFire);
        }
    }

    private double normalizeRelativeAngle(double angle){
        angle = angle % 360;
        if (angle > 180){
            angle -= 360;
        }else if (angle < -180){
            angle += 360;
        }
        return angle;
    }

    private double normalizeAbsoluteAngle(double angle){
        return (angle % 360 + 360) % 360;
    }
}
