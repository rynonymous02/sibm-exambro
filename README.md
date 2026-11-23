# SIBM Exambrowser Desktop (Kiosk Mode)

Aplikasi klien ujian khusus komputer dan laptop (Windows) untuk mengakses sesi Ujian Santri dan Ujian Masuk Seleksi Santri Baru (PSBM) secara aman dan terproteksi penuh.

---

## Fitur Keamanan & Kiosk Lock

1. **Pemblokiran Pintasan Keyboard Tingkat Rendah (Low-Level Keyboard Hook)**:
   - **Alt + Tab**: Diblokir total (siswa tidak bisa beralih ke aplikasi lain).
   - **Tombol Windows (Win Key)**: Diblokir total (mencegah pembukaan Start Menu, Win+D, Win+Tab, Win+R).
   - **Alt + Esc & Alt + F4**: Diblokir (mencegah penutupan paksa atau minimize jendela).
   - **Ctrl + Esc & Ctrl + Shift + Esc**: Diblokir (mencegah pembukaan Start Menu dan shortcut Task Manager).
   - **F11 & F12**: Diblokir (mencegah toggle fullscreen browser dan inspect element/DevTools).
   - **Menu Konteks (Klik Kanan & Tombol Apps)**: Dinonaktifkan.

2. **Penanganan Ctrl + Alt + Delete**:
   - Menerapkan kebijakan registry Windows (`HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\System`):
     - `DisableTaskMgr = 1` (Task Manager dinonaktifkan).
     - `DisableChangePassword = 1` & `DisableLockWorkstation = 1`.
   - Ketika siswa menekan `Ctrl + Alt + Del`, opsi Task Manager diblokir.
   - Ketika aplikasi ditutup secara resmi, seluruh pengaturan registry otomatis dikembalikan normal.

3. **Pintasan Keluar Aman (Dua Opsi)**:
   - **Pintasan Keluar Admin: `Ctrl + I + T`**  
     Menekan tombol `Ctrl` lalu menekan `I` dan `T` (bersamaan atau berurutan).  
     Aplikasi akan meminta PIN khusus Admin/Panitia.  
     Jika PIN benar, aplikasi akan langsung keluar dan memulihkan pengaturan sistem.
   - **Pintasan Keluar Darurat Pengawas: `Ctrl + X`**  
     Menekan tombol `Ctrl + X`.  
     Aplikasi akan meminta **PIN Pengawas Dinamis** (kode 6 digit aktif dari halaman Panel Pengawas SIBM).  
     Setelah diverifikasi valid oleh server, aplikasi akan keluar secara aman.

4. **Dukungan Penuh Ujian Seleksi Santri Baru (PSBM) & Fleksibilitas Link**:
   - **Tautan Soal Berbeda**: Setiap sesi ujian memiliki tautan soal (`link_gform`) masing-masing. Sesi ujian santri SIBM dan sesi seleksi PSBM dapat mengarah ke link Google Form atau web soal yang berbeda tanpa konflik.
   - **Dukungan Dua Server Sekaligus**: Berkas `config.ini` mendukung `server=` (server utama SIBM) dan `server_psbm=` (server seleksi PSBM). Jika kode 6 digit tidak ditemukan di server SIBM, aplikasi otomatis mencari ke server PSBM secara transparan.
   - **Deteksi Otomatis Link Sesi**: Jika peserta/pengawas menempelkan link URL sesi lengkap, aplikasi otomatis mendeteksi domain server asal dan tokennya.
   - **Tautan Form Langsung**: Jika dimasukkan tautan langsung Google Form atau form ujian eksternal lainnya, aplikasi langsung mengunci ke Kiosk Mode dan membuka soal tersebut.

---

## Panduan Penggunaan

1. Buka berkas `config.ini` di folder aplikasi untuk menyetel alamat server (misal: `server_sibm=http://127.0.0.1:8000` atau IP server madrasah). Pengaturan ini hanya perlu diisi sekali oleh panitia/teknisi lab.
2. Jalankan `SIBM-Exambro.exe`.
3. Pada layar awal:
   - Pengawas/panitia dapat menekan tombol **MASUK KIOSK MODE** secara langsung untuk mengunci layar penuh dan mengaktifkan proteksi keyboard hook sejak di halaman awal.
   - Peserta memasukkan **Kode Sesi (6 Karakter)** dan **Nama Peserta**.
4. Klik **MULAI UJIAN**. Layar wajib masuk ke mode kiosk terkunci penuh secara otomatis saat ujian dimulai.
5. Untuk keluar setelah selesai:
   - Gunakan **Ctrl + I + T** lalu masukkan PIN Admin, ATAU
   - Gunakan **Ctrl + X** lalu masukkan PIN Pengawas aktif dari Panel Pengawas SIBM.
