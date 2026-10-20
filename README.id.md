# Dynamic Island untuk Windows

[English](README.md) | **Bahasa Indonesia**

**Dynamic Island** ala iOS untuk Windows 10 dan 11. Sebuah pill kaca buram ada di tengah atas layar, melebar dengan animasi pegas saat kursor diarahkan ke sana, dan menyediakan kalender, kontrol media, statistik sistem, pusat kontrol, dan lainnya tanpa membuka jendela apa pun.

<!-- TODO: ganti gambar di bawah dengan screenshot asli aplikasi Anda -->
![Showcase](docs/screenshots/00-showcase.png)

## Unduh

[![Unduh rilis terbaru](https://img.shields.io/github/v/release/LitRHap/dynamic-island-windows?label=unduh&style=for-the-badge)](https://github.com/LitRHap/dynamic-island-windows/releases/)

| Versi | Tautan | Kebutuhan |
|---|---|---|
| **Mandiri** (disarankan) | [DynamicIsland-win-x64-standalone.zip](https://github.com/USERNAME/NAMA-REPO/releases/latest/download/DynamicIsland-win-x64-standalone.zip) | Tidak butuh apa pun, ekstrak lalu jalankan (ukuran lebih besar) |
| **Ringan** | [DynamicIsland-win-x64.zip](https://github.com/USERNAME/NAMA-REPO/releases/latest/download/DynamicIsland-win-x64.zip) | [.NET 8 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/8.0) |

Semua versi: [halaman Releases](https://github.com/USERNAME/NAMA-REPO/releases).

**Cara pasang:**

1. Unduh salah satu zip di atas lalu ekstrak ke folder mana pun (mis. `C:\Apps\DynamicIsland`).
2. Jalankan `DynamicIsland.exe`. Island muncul di tengah atas layar.
3. *(Opsional)* Klik kanan island lalu pilih **Mulai otomatis dengan Windows**.

> **Muncul "Windows protected your PC" (SmartScreen)?** Aplikasi ini belum ditandatangani (code signing). Klik **More info → Run anyway**. Kode sumbernya terbuka di repositori ini.

## Fitur

- **Kaca buram (frosted glass)** – island mengaburkan apa pun di belakangnya, dengan tint lembut dan garis tepi kaca. Sisi atas rata dengan tepi layar, sisi bawah melengkung.
- **Gerakan ala iOS** – animasi berbasis pegas yang bisa dibatalkan dengan mulus (kursor masuk lalu keluar, gerakannya berbalik tanpa mengulang dari awal).
- **Arahkan untuk membuka, scroll untuk pindah halaman** – gunakan scroll mouse atau touchpad, atau klik ikon di bagian bawah.
- **Ikon lembut** – semua ikon berupa vektor dengan ujung membulat, tanpa sudut tajam.

### Halaman

| Halaman | Fungsi |
|---|---|
| **Kalender & acara** (default) | Tanggal, deretan 7 hari, dan acara mendatang |
| **Media** | Sedang diputar dari Spotify, browser, dan pemutar lain: sampul, judul, artis, putar/jeda/lewati, progress bar yang bisa diklik |
| **Performa** | CPU, RAM, GPU, dan kecepatan jaringan, diperbarui tiap detik |
| **Pusat Kontrol** | Slider volume dan kecerahan, Wi-Fi, Bluetooth, senyap, screenshot, kunci layar, Pengaturan |
| **Notifikasi** | Notifikasi Windows (lihat batasan) atau pintasan ke Pusat Notifikasi |
| **Timer** | Preset cepat (1 / 5 / 10 / 25 menit), jeda, dan reset |
| **Rak berkas** | Seret file ke island, lalu seret keluar lagi ke mana saja |

### Indikator langsung

- Toast saat volume berubah, charger dicolok/dicabut, dan baterai lemah
- **Pengingat Caps Lock** (berlaku di semua aplikasi) dengan lencana kecil di pill selama aktif
- Sisa waktu timer tampil di pill kecil
- Island **menyelinap ke atas** saat sebuah jendela (misalnya bar tab browser) menutupi areanya; gerakkan kursor ke tepi paling atas untuk memunculkannya lagi

![Kalender](docs/screenshots/02-calendar.png)
![Media](docs/screenshots/03-media.png)
![Performa](docs/screenshots/04-stats.png)
![Pusat Kontrol](docs/screenshots/05-control-center.png)
![Toast Caps Lock](docs/screenshots/06-capslock-toast.png)

## Persyaratan

**Untuk menjalankan aplikasi**

| | |
|---|---|
| OS | Windows 10 (versi 1903 atau lebih baru) atau Windows 11, 64-bit |
| Runtime | Tidak perlu untuk unduhan *Mandiri*. Unduhan *Ringan* butuh [.NET 8 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Izin | Tidak perlu hak administrator |
| Perangkat opsional | Laptop atau layar internal (slider kecerahan), adaptor Wi-Fi / Bluetooth (tombol), GPU dengan performance counter Windows (statistik GPU) |

**Untuk build dari kode sumber**

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (17.8+) dengan workload *.NET desktop development*, atau VS Code
- Internet saat build pertama (paket NuGet diunduh otomatis)

## Build dari kode sumber

```bash
git clone https://github.com/USERNAME/NAMA-REPO.git
cd NAMA-REPO
dotnet run
```

Membuat versi mandiri:

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

File `.exe` ada di `bin/Release/net8.0-windows10.0.19041.0/win-x64/publish/`.

## Cara pakai

| Aksi | Hasil |
|---|---|
| Arahkan kursor ke pill | Melebar (dibuka di halaman Kalender) |
| Scroll mouse / touchpad | Halaman berikutnya / sebelumnya |
| Klik ikon di bagian bawah | Langsung ke halaman itu |
| Seret file ke island | Membuka rak berkas |
| Gerakkan kursor ke tepi paling atas | Memunculkan island yang sedang bersembunyi |
| Klik kanan island | Mulai otomatis dengan Windows, Keluar |

## Kustomisasi

Konstanta di bagian atas `MainWindow.xaml.cs`:

- `GlassA`, `GlassR`, `GlassG`, `GlassB` – kepekatan dan warna kaca
- `Jelly` – 0 = tenang ala iOS, 1 = bergelombang seperti air
- `AutoHideWhenCovered` – nyalakan/matikan perilaku menyelinap ke atas
- `CW`, `CH`, `EW`, … – ukuran

## Cara kerja

- Jendela WPF transparan per piksel dengan ukuran tetap; bentuk island digambar dan dianimasikan oleh WPF, jadi tidak ada perubahan ukuran jendela dan tidak berkedip.
- Kaca: area di belakang island ditangkap, dikecilkan, diburamkan, dan dipertajam warnanya di CPU, lalu dipotong sesuai bentuk island.
- Fisika pegas mengatur ukuran dan posisi tiap frame.
- Media lewat Windows Global System Media Transport Controls, audio lewat NAudio, Caps Lock lewat hook keyboard tingkat rendah, statistik GPU lewat performance counter Windows.

## Pemecahan masalah

- **Peringatan SmartScreen** – lihat catatan di bagian *Unduh*; aplikasinya belum ditandatangani.
- **Antivirus menandai aplikasi** – pengingat Caps Lock memakai hook keyboard tingkat rendah yang hanya bereaksi pada tombol Caps Lock. Kodenya terbuka untuk diperiksa.
- **Pengingat Caps Lock tidak bereaksi saat jendela "Run as administrator" sedang aktif** – Windows tidak mengizinkan aplikasi biasa melihat tombol yang diketik di jendela ber-hak admin.
- **Island tidak terlihat** – cek apakah ia bersembunyi di balik jendela yang di-maximize; gerakkan kursor ke tepi paling atas layar.

## Batasan yang diketahui

- **Membaca notifikasi Windows** membutuhkan aplikasi berpaket (MSIX). Saat dijalankan tanpa paket, halaman Notifikasi menampilkan tombol untuk membuka Pusat Notifikasi Windows.
- Slider **kecerahan** hanya muncul di laptop / layar internal.
- **GPU** memakai performance counter Windows dan bisa tampil `N/A` di sebagian sistem.
- Hanya monitor utama.

## Lisensi

[MIT](LICENSE)

<!-- TODO: tambahkan penjelasan fitur lain di sini (mis. baterai Bluetooth, pemantau unduhan) -->
