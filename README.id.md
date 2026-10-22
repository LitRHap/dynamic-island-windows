# Dynamic Island untuk Windows

[English](README.md) | **Bahasa Indonesia**

**Dynamic Island** ala iOS untuk Windows 10 dan 11. Sebuah pill kaca buram ada di tengah atas layar, melebar dengan animasi pegas saat kursor diarahkan ke sana, dan menyediakan kalender, kontrol media dengan lirik, statistik sistem, pusat kontrol, dan lainnya tanpa membuka jendela apa pun.

![Showcase](docs/screenshots/00-showcase.png)

## Unduh

<p align="center">
  <a href="https://github.com/LitRHap/dynamic-island-windows/releases/latest/download/DynamicIsland.exe">
    <img src="https://img.shields.io/badge/Unduh-DynamicIsland.exe-0a84ff?style=for-the-badge&logo=windows&logoColor=white" alt="Unduh DynamicIsland.exe">
  </a>
  <br>
  <a href="https://github.com/LitRHap/dynamic-island-windows/releases/latest">
    <img src="https://img.shields.io/github/v/release/LitRHap/dynamic-island-windows?label=versi%20terbaru" alt="Versi terbaru">
  </a>
</p>

**Cara pasang:**

1. Unduh **[DynamicIsland.exe](https://github.com/LitRHap/dynamic-island-windows/releases/latest/download/DynamicIsland.exe)**. Hanya satu file; tanpa installer dan tanpa runtime tambahan.
2. Taruh di folder mana pun (mis. `C:\Apps\DynamicIsland`) lalu jalankan. Island muncul di tengah atas layar.
3. *(Opsional)* Klik kanan island lalu pilih **Mulai otomatis dengan Windows**.

> **Muncul "Windows protected your PC" (SmartScreen)?** Aplikasi ini belum ditandatangani (code signing). Klik **More info → Run anyway**. Kode sumbernya terbuka di [GitHub](https://github.com/LitRHap/dynamic-island-windows).

Versi lama ada di [halaman Releases](https://github.com/LitRHap/dynamic-island-windows/releases). Perubahan tiap versi ada di [Changelog](CHANGELOG.md).

## Fitur

- **Kaca buram (frosted glass)**: island mengaburkan apa pun di belakangnya, dengan tint lembut dan garis tepi kaca. Sisi atas rata dengan tepi layar, sisi bawah melengkung.
- **Gerakan ala iOS**: animasi berbasis pegas yang bisa dibatalkan dengan mulus (kursor masuk lalu keluar, gerakannya berbalik tanpa mengulang dari awal).
- **Arahkan untuk membuka, scroll untuk pindah halaman**: gunakan scroll mouse atau touchpad, atau klik ikon di bagian bawah.
- **Ikon lembut**: semua ikon berupa vektor dengan ujung membulat, tanpa sudut tajam.

### Halaman

| Halaman | Fungsi |
|---|---|
| **Kalender & acara** (default) | Kalender bulanan, daftar acara pada tanggal terpilih, dan tombol **+** untuk menambah acara |
| **Media** | Sedang diputar dari Spotify, browser, dan pemutar lain: sampul, judul, artis, putar/jeda/lewati, progress bar yang bisa diklik, dan **lirik tersinkron** (bisa dimatikan) |
| **Performa** | Nama Wi-Fi yang tersambung beserta pemakaian datanya, plus CPU, RAM, GPU, dan kecepatan jaringan, diperbarui tiap detik |
| **Pusat Kontrol** | Slider volume dan kecerahan, Wi-Fi, Bluetooth, senyap, screenshot, kunci layar, Pengaturan |
| **Notifikasi** | Notifikasi Windows (lihat batasan) atau pintasan ke Pusat Notifikasi |
| **Timer** | Preset cepat (1 / 5 / 10 / 25 menit), jeda, dan reset |
| **Rak berkas** | Seret file ke island, lalu seret keluar lagi ke mana saja |

### Indikator langsung

- Toast saat volume berubah, charger dicolok/dicabut, dan baterai lemah
- **Pengingat Caps Lock** (berlaku di semua aplikasi) dengan lencana kecil di pill selama aktif
- Sisa waktu timer tampil di pill kecil
- **Baterai Bluetooth**: toast berisi level baterai saat headset terhubung (jika perangkat melaporkannya ke Windows), plus indikator kecil di island
- **Indikator unduhan**: garis yang bergerak dan kecepatan saat unduhan browser berjalan, plus toast saat selesai
- **Pengingat istirahat**: setelah 60 menit terus-menerus memakai komputer, island mengingatkan Anda untuk mengistirahatkan mata. Jedanya (30 / 45 / 60 / 90 menit, atau mati) diatur dari menu klik kanan. Menjauh dari keyboard dan mouse selama 3 menit dianggap sudah istirahat, dan hitungan dimulai ulang.
- Island **menyelinap ke atas** saat sebuah jendela (misalnya bar tab browser) menutupi areanya; gerakkan kursor ke tepi paling atas untuk memunculkannya lagi

![Kalender](docs/screenshots/02-calendar.png)
![Media](docs/screenshots/03-media.png)
![Performa](docs/screenshots/04-stats.png)
![Pusat Kontrol](docs/screenshots/05-control-center.png)
![Toast Caps Lock](docs/screenshots/06-capslock-toast.png)
![Pengingat istirahat](docs/screenshots/08-break-toast.png)

## Persyaratan

| | |
|---|---|
| **OS** | Windows 10 versi 2004 (build 19041) atau lebih baru, atau Windows 11. 64-bit |
| **Runtime** | Tidak perlu. `DynamicIsland.exe` sudah membawa semua yang dibutuhkan |
| **Izin** | Tidak perlu hak administrator |
| **Internet** | Hanya untuk lirik. Fitur lainnya berjalan offline |
| **Perangkat opsional** | Laptop atau layar internal (slider kecerahan), adaptor Wi-Fi / Bluetooth (tombol, nama Wi-Fi), GPU dengan performance counter Windows (statistik GPU) |

## Cara pakai

| Aksi | Hasil |
|---|---|
| Arahkan kursor ke pill | Melebar (dibuka di halaman Kalender) |
| Scroll mouse / touchpad | Halaman berikutnya / sebelumnya |
| Klik ikon di bagian bawah | Langsung ke halaman itu |
| Klik tanggal, tekan **+** | Melihat atau menambah acara pada hari itu (ketik `14:00 Rapat tim`, lalu Enter) |
| Seret file ke island | Membuka rak berkas |
| Gerakkan kursor ke tepi paling atas | Memunculkan island yang sedang bersembunyi |
| Klik kanan island | Mulai otomatis dengan Windows, **tampilkan/sembunyikan lirik**, **jeda pengingat istirahat**, Keluar |

Acara Anda tersimpan di `%AppData%\DynamicIsland\events.json`.

## Lirik

Lirik tersinkron tampil di halaman Media dan mengikuti posisi lagu. Lirik diambil saat program berjalan dari [LRCLIB](https://lrclib.net), database lirik gratis berbasis komunitas, lalu disimpan di cache `%AppData%\DynamicIsland\lyrics`. Yang dikirim hanya judul dan artis lagu. Tidak semua lagu punya lirik; kalau tidak ditemukan, panelnya disembunyikan. Fitur ini bisa dinyalakan atau dimatikan dari menu klik kanan. Pilihan Anda diingat di `%AppData%\DynamicIsland\prefs.json`.

## Pemecahan masalah

- **Peringatan SmartScreen**: lihat catatan di bagian *Unduh*; aplikasinya belum ditandatangani.
- **Antivirus menandai aplikasi**: pengingat Caps Lock memakai hook keyboard tingkat rendah yang hanya bereaksi pada tombol Caps Lock. Kodenya terbuka untuk diperiksa.
- **Pengingat Caps Lock tidak bereaksi saat jendela "Run as administrator" sedang aktif**: Windows tidak mengizinkan aplikasi biasa melihat tombol yang diketik di jendela ber-hak admin.
- **Island tidak terlihat**: cek apakah ia bersembunyi di balik jendela yang di-maximize; gerakkan kursor ke tepi paling atas layar.
- **Lirik tidak muncul**: lagunya mungkin tidak ada di LRCLIB, atau judul/artis dari pemutar tidak biasa (mis. judul YouTube yang panjang).

## Batasan yang diketahui

- **Membaca notifikasi Windows** membutuhkan aplikasi berpaket (MSIX). Saat dijalankan tanpa paket, halaman Notifikasi menampilkan tombol untuk membuka Pusat Notifikasi Windows.
- Slider **kecerahan** hanya muncul di laptop / layar internal.
- **GPU** memakai performance counter Windows dan bisa tampil `N/A` di sebagian sistem.
- **Pemakaian data Wi-Fi** berasal dari statistik "Penggunaan data" Windows, yang hanya menyimpan 30 hari terakhir.
- **Baterai Bluetooth** hanya tampil jika perangkat melaporkan baterainya ke Windows. Kalau tidak tampil di *Settings → Bluetooth & devices*, di sini juga tidak.
- **Indikator unduhan** menampilkan aktivitas dan kecepatan, bukan persentase: browser tidak memberi tahu ukuran total file ke Windows.
- Hanya monitor utama.

## Kode sumber

Kode sumber lengkap, issue, dan diskusi ada di GitHub:
**[github.com/LitRHap/dynamic-island-windows](https://github.com/LitRHap/dynamic-island-windows)**

## Kredit

- Lirik disediakan oleh [LRCLIB](https://lrclib.net)
- Audio lewat [NAudio](https://github.com/naudio/NAudio)

## Lisensi

[MIT](LICENSE)
