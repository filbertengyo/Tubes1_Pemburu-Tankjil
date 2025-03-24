<h1 align="center">Tugas Besar 1 IF2211 Strategi Algoritma</h1>
<h3 align="center">Pembuatan Bot Robocode dengan Algoritma Greedy</p>

## Daftar Isi

- [Overview](#overview)
- [Requirements](#requirements)
- [Installation](#installation)
- [Author](#author)

## Overview

Robocode adalah permainan pemrograman yang bertujuan untuk membuat kode bot dalam bentuk tank virtual untuk berkompetisi melawan bot lain di arena. Pertempuran Robocode berlangsung hingga bot-bot bertarung hanya tersisa satu seperti permainan Battle Royale, karena itulah permainan ini dinamakan Tank Royale. Nama Robocode adalah singkatan dari "Robot code," yang berasal dari versi asli/pertama permainan ini. Robocode Tank Royale adalah evolusi/versi berikutnya dari permainan ini, di mana bot dapat berpartisipasi melalui Internet/jaringan.

Dalam permainan ini, pemain berperan sebagai programmer bot dan tidak memiliki kendali langsung atas permainan. Pemain hanya bertugas untuk membuat program yang menentukan logika atau "otak" bot. Program yang dibuat akan berisi instruksi tentang cara bot bergerak, mendeteksi bot lawan, menembakkan senjatanya, serta bagaimana bot bereaksi terhadap berbagai kejadian selama pertempuran.

Kelompok Pemburu Tankjil telah membuat 4 bot dengan algoritma _greedy_ sebagai berikut:
### Bhh
Bot yang akan terus mengejar dan menembak musuh tanpa ampun. Bot ini akan menghasilkan _ram score_ yang tinggi jika berhasil menyudutkan musuh.

### Chh
Bot ini akan berusaha menghindar dari serangan musuh terdekat. Selain menghindar, bot Chh juga akan menyerang bot musuh berdasarkan probabilitas hit yang tertinggi.

### PemburuAlatreon
Bot menggunakan pendekatan _greedy_ dengan heuristik mengincar target terdekat dan memaksimalkan firepower untuk jarak paling dekat. Alternatif ini muncul karena dengan mengincar bot terdekat, seharusnya bot tersebut dapat lebih mudah mengenai bot musuh.

### DrinkAndDrive
Bot DrinkAndDrive dibuat dengan motif mengincar musuh dengan energy atau nyawa terendah yang tergantung pada jaraknya juga akan memaksimalkan serangan tembakan.

## Requirements
- Pastikan .NET 9.0 SDK (dan runtime-nya) telah terinstall.
- Pastikan _engine_ robocode yang telah dimodifikasi ada.

## Installation

Untuk menjalankan program, maka lakukan langkah berikut:

1. Klon repositori ini ke lokal:
```shell
git clone https://github.com/filbertengyo/Tubes1_Pemburu-Tankjil.git
```

2. Jalankan _engine_ robocode:
```shell
java -jar robocode-tankroyale-gui-0.30.0.jar  # Pastikan di folder yang sama dengan file .jar
```

3. Tambahkan direktori folder `src` kedalam konfigurasi direktori bot robocode. Untuk menambahkan bot alternatif, tambahkan juga folder `alternative-bots`.
4. Boot bot dan mulai _battle_!

Pastikan requirements terpenuhi sebelum menjalankan program.

## Author
- [Muhammad Aulia Azka](https://github.com/Azzkaaaa) - 13523137 - K3
- [Fachriza Ahmad Setiyono](https://github.com/L4mbads) - 13523162 - K3
- [Filbert Engyo](https://github.com/filbertengyo) - 13523163 - K3
