<p align="center">
  <img src="beyazlogo.png" alt="TicketFlow" width="520">
</p>

# TicketFlow

TicketFlow, teknik destek taleplerini oluşturmak, takip etmek ve destek ekibi tarafından yönetmek için geliştirilmiş ASP.NET Core MVC tabanlı bir ticket sistemidir.

Müşteriler destek talebi oluşturabilir, taleplerinin durumunu izleyebilir. Destek ekibi ise talepleri üstlenebilir, cevap yazabilir, durum güncelleyebilir ve gerektiğinde talepleri silebilir.

## Özellikler

- Müşteri destek talebi oluşturma
- Müşterinin kendi taleplerini ve durumlarını izleyebilmesi
- Destek ekibinin tüm talepleri görebilmesi
- Talepleri üstlenme
- Talebe cevap yazma
- Talep durumunu güncelleme
- Talep silme
- Duruma göre filtreleme
- Rol bazlı yetkilendirme
- Modern, responsive landing page ve ortak layout

## Talep Durumları

TicketFlow durum yönetimi için `TicketStatus` enum tipini kullanır:

- `Open` - Açık
- `Resolved` - Çözüldü
- `Closed` - Kapandı

## Teknik Katmanlar

- ASP.NET Core MVC (.NET 9)
- ASP.NET Core Identity ile authentication
- `Customer`, `Support`, `Admin` rolleri ile authorization
- EF Core Code-First yaklaşımı
- SQLite veritabanı
- DbContext ve migrations
- LINQ ile kullanıcı ve durum bazlı filtreleme
- ViewModel kullanımı
- Data Annotations ile form validasyonları
- Ortak `_Layout.cshtml` header/footer yapısı
- Bootstrap tabanlı responsive arayüz
- GSAP ScrollTrigger ile landing page mikro animasyonları

## CRUD Kapsamı

Ticket işlemleri temel CRUD kapsamını karşılar:

- Create: Yeni destek talebi oluşturma
- Read: Talep listesi ve talep detayını görüntüleme
- Update: Durum güncelleme, destek sorumlusu atama, cevap ekleme
- Delete: Talep silme

## Roller

| Rol | Yetkiler |
| --- | --- |
| Customer | Talep oluşturur, kendi taleplerini görür, cevap yazar, kendi talebini silebilir |
| Support | Tüm talepleri görür, üstlenir, cevap yazar, durum günceller, silebilir |
| Admin | Support yetkilerine ek olarak yönetici rolüyle sistemde yer alır |

## Demo Kullanıcılar

| Rol | E-posta | Şifre |
| --- | --- | --- |
| Customer | `customer@ticketflow.local` | `Customer123!` |
| Support | `support@ticketflow.local` | `Support123!` |
| Admin | `admin@ticketflow.local` | `Admin123!` |

## Kurulum ve Çalıştırma

```powershell
dotnet restore
dotnet tool restore
dotnet tool run dotnet-ef database update
dotnet run
```

Uygulama varsayılan olarak local geliştirme adreslerinde çalışır. Bu çalışma sırasında sıklıkla kullanılan adres:

```text
http://localhost:5177
```

## Veritabanı

Proje SQLite kullanır. Bağlantı bilgisi `appsettings.json` içindedir:

```json
"DefaultConnection": "DataSource=app.db;Cache=Shared"
```

EF Core migration dosyaları `Data/Migrations` altında tutulur.

## Proje Yapısı

```text
Controllers/        MVC controller katmanı
Data/               DbContext, seed verisi ve migrations
Models/             Entity ve enum modelleri
ViewModels/         Sayfalara veri taşıyan ViewModel sınıfları
Views/              Razor view dosyaları
Areas/Identity/     Identity kayıt ve hesap sayfaları
wwwroot/            CSS, JS, görseller ve statik dosyalar
```

## Validasyonlar

Form kontrollerinde Data Annotations kullanılır:

- `Required`
- `StringLength`
- `EmailAddress`
- `Compare`

Bu sayede kayıt, talep oluşturma ve cevap yazma formları kullanıcı hatalarına karşı kontrol edilir.
