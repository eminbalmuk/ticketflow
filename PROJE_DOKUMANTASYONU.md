# TicketFlow Proje Dokumantasyonu

Bu dosya, TicketFlow projesinin amacini, ASP.NET Core MVC yapisini, temel dosyalarin gorevlerini ve veritabani ile nasil baglanti kurdugunu aciklar.

## Projenin Amaci

TicketFlow, musterilerin teknik destek talepleri olusturabildigi, destek ekibinin bu talepleri takip edip cevaplayabildigi ve yoneticinin destek kullanicilarini yonetebildigi bir ticket sistemidir.

Proje, ASP.NET Core MVC ve ASP.NET Core Identity uzerine kuruludur. Veriler Entity Framework Core Code First yaklasimi ile SQL Server veritabaninda tutulur.

## Proje Ne Yapar?

- Musteri kullanicilari sisteme kayit olabilir ve giris yapabilir.
- Musteriler yeni destek talebi acabilir.
- Talepler baslik, aciklama, kategori, durum, musteri ve destek sorumlusu bilgileriyle saklanir.
- Musteri kendi taleplerini listeleyebilir, detaylarini gorebilir, cevap yazabilir ve talebini silebilir.
- Support kullanicilari yetkili olduklari kategorilerdeki talepleri gorebilir.
- Support kullanicilari talebi ustlenebilir, cevap yazabilir ve talep durumunu guncelleyebilir.
- Admin kullanicisi tum talepleri gorebilir, destek sorumlusu atayabilir, support kullanicisi olusturabilir ve support kategori yetkilerini duzenleyebilir.
- Talep acma, cevap yazma, ustlenme, atama ve durum guncelleme gibi islemlerde ilgili kullanicilara bildirim olusturulur.

## Kullanici Rolleri

Projede uc temel rol vardir:

| Rol | Aciklama |
| --- | --- |
| `Customer` | Normal musteri roludur. Talep acar, kendi taleplerini gorur ve cevap yazar. |
| `Support` | Destek personeli roludur. Yetkili oldugu kategorilerdeki talepleri yonetir. |
| `Admin` | Yonetici roludur. Tum talepleri, support kullanicilarini ve kategori yetkilerini yonetir. |

Bu roller `Data/SeedData.cs` icinde tanimlanir ve uygulama acilisinda yoksa otomatik olusturulur.

## Kullanilan Teknolojiler

- ASP.NET Core MVC
- .NET 9
- Razor Views ve Razor Pages
- ASP.NET Core Identity
- Entity Framework Core
- SQL Server
- EF Core Migrations
- Bootstrap
- jQuery Validation
- GSAP ve ScrollTrigger

Paketler `ticketflow.csproj` dosyasinda tanimlidir. Projede ozellikle su paketler kullanilir:

- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Microsoft.AspNetCore.Identity.UI`
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Tools`
- `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore`

## ASP.NET Core Baslangic Akisi

Uygulamanin ana giris dosyasi `Program.cs` dosyasidir.

Bu dosyada yapilan baslica islemler:

1. `appsettings.json` icinden `DefaultConnection` baglanti cumlesi okunur.
2. `ApplicationDbContext`, SQL Server kullanacak sekilde servis koleksiyonuna eklenir.
3. ASP.NET Core Identity, `ApplicationUser` sinifi ile kurulur.
4. Role destegi `AddRoles<IdentityRole>()` ile aktif edilir.
5. MVC controller ve view destegi `AddControllersWithViews()` ile eklenir.
6. Authentication ve authorization middleware'leri aktif edilir.
7. Varsayilan route su sekilde tanimlanir:

```text
{controller=Home}/{action=Index}/{id?}
```

8. Identity Razor Pages rotalari `MapRazorPages()` ile eklenir.
9. Uygulama baslarken `MigrateAsync()` ile migration'lar veritabanina uygulanir.
10. `SeedData.InitializeAsync()` ile roller ve demo kullanicilar olusturulur.

## `Program.cs` Detayli Aciklama

`Program.cs`, ASP.NET Core uygulamasinin baslatildigi ana dosyadir. Proje calismaya basladiginda ilk olarak bu dosyadaki kodlar islenir. Bu yuzden uygulamanin hangi servisleri kullanacagi, veritabanina nasil baglanacagi, login/rol sisteminin nasil kurulacagi ve gelen isteklerin hangi controller'a gidecegi burada belirlenir.

Bu projede `Program.cs` iki ana bolum gibi dusunulebilir:

1. Servislerin tanimlandigi bolum
2. HTTP request pipeline'in kuruldugu bolum

### Servislerin Tanimlanmasi

Servis tanimlama bolumunde uygulamanin kullanacagi altyapi parcalari dependency injection sistemine eklenir.

Veritabani baglantisi burada okunur:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
```

Bu satir `appsettings.json` icindeki `DefaultConnection` degerini alir. Eger bu deger bulunamazsa uygulama hata verir.

Sonra `ApplicationDbContext` SQL Server ile calisacak sekilde eklenir:

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
```

Bu sayede controller'lar constructor uzerinden `ApplicationDbContext` isteyebilir:

```csharp
public TicketsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
{
    _context = context;
    _userManager = userManager;
}
```

Identity sistemi de yine `Program.cs` icinde kurulur:

```csharp
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
```

Bu kodun anlami:

- Kullanici modeli olarak `ApplicationUser` kullanilir.
- E-posta onayi zorunlu degildir.
- Rol sistemi aktif edilir.
- Identity verileri `ApplicationDbContext` uzerinden veritabaninda tutulur.

MVC destegi de su satirla eklenir:

```csharp
builder.Services.AddControllersWithViews();
```

Bu satir olmadan `Controllers/` ve `Views/` klasorleri MVC mantigiyla calismaz.

### Middleware ve Route Yapisi

`Program.cs` icindeki ikinci bolum gelen HTTP isteklerinin hangi sirayla islenecegini belirler.

Onemli middleware'ler:

```csharp
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
```

Sirasi onemlidir:

- `UseRouting()`: Gelen URL'nin hangi route'a uydugunu bulur.
- `UseAuthentication()`: Kullanicinin giris yapip yapmadigini kontrol eder.
- `UseAuthorization()`: Kullanicinin ilgili sayfaya yetkisi olup olmadigini kontrol eder.

Varsayilan MVC route'u sudur:

```csharp
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

Bu su anlama gelir:

```text
/Tickets/Details/5
```

istegi geldiginde ASP.NET Core bunu su sekilde yorumlar:

```text
Controller: TicketsController
Action: Details
id: 5
```

Identity sayfalari ise Razor Pages olarak calistigi icin ayrica map edilir:

```csharp
app.MapRazorPages();
```

Uygulama baslarken veritabani da hazirlanir:

```csharp
await ApplyMigrationsAndSeedAsync(app.Services);
```

Bu metot icinde:

- `dbContext.Database.MigrateAsync()` ile migration'lar uygulanir.
- `SeedData.InitializeAsync()` ile roller ve demo kullanicilar olusturulur.

## ASP.NET Dosya ve Klasor Yapisi

| Dosya/Klasor | Gorevi |
| --- | --- |
| `Program.cs` | Uygulamanin servislerini, middleware'lerini, route yapisini, migration ve seed islemlerini baslatir. |
| `ticketflow.csproj` | Projenin .NET surumunu ve NuGet paketlerini tanimlar. |
| `appsettings.json` | SQL Server baglanti cumlesi ve log ayarlarini tutar. |
| `Properties/launchSettings.json` | Local calistirma profillerini ve portlari tanimlar. |
| `Controllers/` | MVC controller siniflari bulunur. HTTP isteklerini karsilar ve gerekli view/model sonucunu dondurur. |
| `Models/` | Veritabanina karsilik gelen entity siniflari, enum'lar ve yardimci model siniflari bulunur. |
| `ViewModels/` | View'lara ozel veri tasima siniflari bulunur. Entity'lerin ekrana dogrudan verilmesini azaltir. |
| `Views/` | Razor view dosyalari bulunur. MVC ekranlari burada olusturulur. |
| `Areas/Identity/` | Login, register ve hesap yonetimi gibi Identity Razor Page dosyalari bulunur. |
| `Data/` | `ApplicationDbContext`, seed verileri ve EF Core migration dosyalari bulunur. |
| `wwwroot/` | CSS, JavaScript, gorseller ve frontend kutuphaneleri gibi statik dosyalar bulunur. |
| `publish/` | Yayina alma sonucu uretilmis derlenmis ciktilari icerir. Kaynak kodun ana mantigi burada degildir. |

## MVC Akisi

Bu proje ASP.NET Core MVC yapisini kullanir. MVC, `Model`, `View` ve `Controller` parcalarindan olusur.

Genel akis su sekildedir:

```text
Kullanici tarayicidan istek gonderir
        v
Program.cs route ayarina gore controller secilir
        v
Controller action metodu calisir
        v
Gerekirse ApplicationDbContext ile veritabanina gidilir
        v
ViewModel hazirlanir
        v
Razor View ekrana HTML olarak basilir
```

Ornek:

```text
/Tickets/Create
```

istegi geldiginde:

1. Route sistemi `TicketsController` sinifini bulur.
2. `Create()` action'i calisir.
3. Controller `TicketCreateViewModel` olusturur.
4. `Views/Tickets/Create.cshtml` dosyasi acilir.
5. Kullanici formu doldurup gonderince `[HttpPost] Create(...)` action'i calisir.
6. Veri dogruysa `Ticket` entity'si olusturulur.
7. `_context.Tickets.Add(ticket)` ile veritabanina eklenir.
8. `SaveChangesAsync()` ile kayit SQL Server'a yazilir.

## Controller Dosyalari

Controller, kullanicidan gelen HTTP isteklerini karsilayan C# sinifidir. Controller icindeki public metotlara `action` denir. Her action belirli bir sayfayi acabilir, form verisini isleyebilir, veritabanindan veri okuyabilir veya kullaniciyi baska sayfaya yonlendirebilir.

Bu projede controller'lar `Controllers/` klasorundedir ve genelde su gorevleri yapar:

- Kullanicinin yetkisini kontrol eder.
- Formdan gelen veriyi ViewModel ile alir.
- `ModelState.IsValid` ile form validasyonunu kontrol eder.
- `ApplicationDbContext` ile veritabanindan veri okur veya veri yazar.
- Sonucu bir Razor View'a gonderir.
- Gerekirse `RedirectToAction`, `NotFound`, `Forbid` veya `Challenge` sonucu dondurur.

Controller'lar dependency injection ile ihtiyac duyduklari servisleri constructor uzerinden alir.

Ornek:

```csharp
private readonly ApplicationDbContext _context;
private readonly UserManager<ApplicationUser> _userManager;

public TicketsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
{
    _context = context;
    _userManager = userManager;
}
```

Bu ornekte:

- `_context`: Veritabani islemleri icin kullanilir.
- `_userManager`: Giris yapan kullanici ve rollerle ilgili islemler icin kullanilir.

Controller seviyesinde veya action seviyesinde yetki verilebilir.

Ornek:

```csharp
[Authorize]
public class TicketsController : Controller
```

Bu, `TicketsController` icindeki action'lara sadece giris yapmis kullanicilarin erisebilecegi anlamina gelir.

Admin icin daha sinirli yetki ornegi:

```csharp
[Authorize(Roles = SeedData.AdminRole)]
public class AdminController : Controller
```

Bu ise sadece `Admin` rolundeki kullanicilarin admin controller'a erisebilecegi anlamina gelir.

### `Controllers/HomeController.cs`

Ana sayfa, gizlilik sayfasi ve hata sayfasi gibi genel sayfalari yonetir.

Baslica action'lar:

- `Index()`: Ana sayfayi acar.
- `Privacy()`: Privacy sayfasini acar.
- `Error()`: Hata durumunda hata view modelini dondurur.

### `Controllers/TicketsController.cs`

Projenin ana is mantiginin buyuk kismi bu controller'dadir. Tum action'lar `[Authorize]` ile korunur, yani giris yapmayan kullanici ticket ekranlarina erisemez.

Baslica action'lar:

- `Index(TicketStatus? status, bool onlyMine)`: Ticket listesini getirir. Role gore filtreleme yapar.
- `Create()`: Yeni ticket formunu acar.
- `Create(TicketCreateViewModel model)`: Yeni ticket kaydeder.
- `Details(int id)`: Ticket detayini, cevaplari ve aksiyonlari gosterir.
- `Take(int id)`: Support kullanicisinin talebi ustlenmesini saglar.
- `AssignSupport(int id, string? supportUserId)`: Admin'in destek sorumlusu atamasini saglar.
- `Reply(int id, TicketReplyViewModel model)`: Talebe cevap ekler.
- `UpdateStatus(int id, TicketStatus status)`: Talep durumunu gunceller.
- `Delete(int id)`: Yetkisi olan kullanicinin talebi silmesini saglar.

Bu controller ayrica yetki kontrolu icin yardimci metotlar icerir:

- `CanManageTicketAsync()`
- `CanViewAsync()`
- `CanDeleteAsync()`
- `SupportCanHandleCategoryAsync()`
- `GetAllowedCategoriesAsync()`

Bildirim olusturma mantigi da `AddTicketNotificationsAsync()` metodu ile burada uygulanir.

### `Controllers/AdminController.cs`

Sadece `Admin` rolundeki kullanicilar erisebilir:

```csharp
[Authorize(Roles = SeedData.AdminRole)]
```

Baslica gorevleri:

- Admin paneli icin ozet verileri hazirlar.
- Musteri arama ve musterinin ticketlarini gosterme islemini yapar.
- Var olan veya yeni bir kullaniciya `Support` rolu verir.
- Support kullanicilarinin bakabilecegi ticket kategorilerini duzenler.

### `Controllers/CustomersController.cs`

`Admin` ve `Support` rollerinin musteri aramasi yapmasini saglar:

```csharp
[Authorize(Roles = SeedData.AdminRole + "," + SeedData.SupportRole)]
```

Support kullanicisi sadece yetkili oldugu kategorilerdeki talepleri gorecek sekilde filtrelenir.

### `Controllers/NotificationsController.cs`

Bildirim islemlerini yonetir.

Baslica action'lar:

- `Open(int id)`: Bildirimi okundu olarak isaretler ve ilgili ticket detayina yonlendirir.
- `Delete(int id, string? returnUrl)`: Bildirimi siler ve kullaniciyi onceki sayfaya veya ticket listesine dondurur.

## Model Dosyalari

### `Models/ApplicationUser.cs`

ASP.NET Identity kullanicisini genisletir:

```csharp
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
```

Standart Identity alanlarina ek olarak `FullName` alanini ekler.

### `Models/Ticket.cs`

Destek talebini temsil eder.

Onemli alanlar:

- `Id`
- `Title`
- `Description`
- `Status`
- `Category`
- `CreatedAt`
- `UpdatedAt`
- `CustomerId`
- `AssignedSupportId`
- `Replies`

`CustomerId`, talebi acan kullaniciya baglanir. `AssignedSupportId`, talebi ustlenen veya admin tarafindan atanan destek kullanicisini tutar.

### `Models/TicketReply.cs`

Ticket uzerindeki cevaplari temsil eder.

Onemli alanlar:

- `TicketId`
- `AuthorId`
- `Message`
- `CreatedAt`

Her cevap bir ticket'a ve cevabi yazan kullaniciya baglidir.

### `Models/TicketNotification.cs`

Kullanici bildirimlerini temsil eder.

Onemli alanlar:

- `UserId`
- `TicketId`
- `Title`
- `Message`
- `CreatedAt`
- `ReadAt`

`ReadAt` bos ise bildirim okunmamis kabul edilir.

### `Models/SupportCategoryAssignment.cs`

Support kullanicilarinin hangi kategorilerde talep gorebilecegini belirler.

Alanlar:

- `SupportUserId`
- `Category`

Bu tabloda bir support kullanicisi icin birden fazla kategori atanabilir.

### Enum Dosyalari

`TicketStatus.cs` destek talebinin durumlarini tutar:

- `Open`
- `Resolved`
- `Closed`

`TicketCategory.cs` destek talebinin kategorilerini tutar:

- `Phone`
- `Tablet`
- `Camera`
- `Headphones`
- `Television`
- `Monitor`
- `HomeAppliance`

`TicketStatusExtensions.cs` ve `TicketCategoryExtensions.cs`, enum degerlerinin ekranda daha okunabilir isimlerle gosterilmesini saglar.

## ViewModel Dosyalari

ViewModel'ler, controller ile view arasinda tasinacak veriyi duzenler. Bu sayede view'lar sadece ihtiyaci olan veriyi alir.

| Dosya | Gorevi |
| --- | --- |
| `TicketCreateViewModel.cs` | Ticket olusturma formundaki baslik, kategori ve aciklama alanlarini tasir. |
| `TicketListViewModel.cs` | Ticket listeleme ekrani icin filtreler, sayaclar ve liste elemanlarini tasir. |
| `TicketDetailsViewModel.cs` | Ticket detay ekrani icin ticket bilgisi, cevaplar, yetki bayraklari ve support seceneklerini tasir. |
| `TicketReplyViewModel.cs` | Talebe cevap yazma formundaki mesaj alanini tasir. |
| `AdminDashboardViewModel.cs` | Admin panelindeki musteri arama, support kullanicilari ve ozet sayaclari tasir. |
| `CustomerSearchViewModel.cs` | Musteri arama ekrani ve sonuc ticketlarini tasir. |
| `NotificationMenuItemViewModel.cs` | Header'daki bildirim menusu icin bildirim satiri verisini tasir. |

## View Dosyalari

`Views/` klasoru MVC ekranlarini icerir.

| Dosya/Klasor | Gorevi |
| --- | --- |
| `Views/Shared/_Layout.cshtml` | Tum sayfalarda kullanilan ana HTML iskeleti, navbar, footer, CSS/JS referanslari. |
| `Views/Shared/_LoginPartial.cshtml` | Giris/cikis alani, hesap linki ve bildirim kutusunu olusturur. |
| `Views/Shared/_ValidationScriptsPartial.cshtml` | Client-side form validasyon scriptlerini ekler. |
| `Views/_ViewImports.cshtml` | Razor view'larda ortak `using` ifadelerini ve tag helper'lari tanimlar. |
| `Views/_ViewStart.cshtml` | Varsayilan layout dosyasini belirler. |
| `Views/Home/Index.cshtml` | Ana/landing sayfasidir. |
| `Views/Tickets/Index.cshtml` | Ticket listeleme ve filtreleme ekranidir. |
| `Views/Tickets/Create.cshtml` | Yeni ticket olusturma formudur. |
| `Views/Tickets/Details.cshtml` | Ticket detay, cevaplar, durum guncelleme, ustlenme ve silme ekranidir. |
| `Views/Admin/Index.cshtml` | Admin paneli, musteri arama ve support yonetimi ekranidir. |
| `Views/Admin/EditSupportCategories.cshtml` | Support kullanicisinin kategori yetkilerini duzenleme ekranidir. |
| `Views/Customers/Index.cshtml` | Admin/support icin musteri arama ekranidir. |

## Razor View Detayli Aciklama

Razor View dosyalari `.cshtml` uzantili dosyalardir. Bu dosyalar HTML ile C# kodunu birlikte kullanarak dinamik sayfalar olusturur. MVC tarafinda controller bir view dondurdugunde, ilgili `.cshtml` dosyasi calisir ve kullaniciya HTML olarak gonderilir.

Ornek:

```csharp
return View(model);
```

Bu kod `TicketsController.Create()` icindeyse ASP.NET Core varsayilan olarak su dosyayi arar:

```text
Views/Tickets/Create.cshtml
```

### Razor View Icindeki `@model`

Bir view dosyasinin basinda genelde hangi ViewModel'i kullanacagi yazilir.

Ornek:

```csharp
@model TicketCreateViewModel
```

Bu, view icinde `Model` nesnesinin `TicketCreateViewModel` tipinde oldugu anlamina gelir.

Bu sayede view icinde su alanlara erisilebilir:

```csharp
Model.Title
Model.Category
Model.Description
```

### Tag Helper Kullanimi

Razor View'larda ASP.NET Core tag helper'lari kullanilir. Tag helper'lar HTML elemanlarini controller, action ve model alanlariyla baglamayi kolaylastirir.

Ornek:

```html
<form asp-action="Create" method="post">
```

Bu form submit edildiginde ayni controller icindeki `Create` action'ina gider.

Input ornegi:

```html
<input asp-for="Title" class="form-control" />
```

Bu kod `TicketCreateViewModel.Title` alanina bagli bir input olusturur.

Validasyon mesaji ornegi:

```html
<span asp-validation-for="Title" class="text-danger"></span>
```

Eger `Title` alani bos birakilirsa veya kurallara uymazsa hata mesaji burada gosterilir.

### Layout ve Partial View

`Views/_ViewStart.cshtml` dosyasi varsayilan layout'u belirler:

```csharp
Layout = "_Layout";
```

Bu nedenle sayfalar genel olarak `Views/Shared/_Layout.cshtml` icindeki ortak tasarim icinde gosterilir. Navbar, footer, CSS ve JavaScript referanslari burada bulunur.

Partial view ise bir sayfanin tekrar kullanilabilir parcasidir.

Ornek:

```csharp
<partial name="_LoginPartial" />
```

Bu kod `Views/Shared/_LoginPartial.cshtml` dosyasini layout icine ekler. Bu projede login/cikis linkleri ve bildirim kutusu bu partial icindedir.

### Script Section

Bazi view dosyalari sayfaya ozel script eklemek icin `Scripts` section'i kullanir.

Ornek:

```csharp
@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

Bu kod client-side form validasyon scriptlerini sayfaya ekler.

### Bu Projede Razor View Akisi

Ticket olusturma ekrani uzerinden ornek:

```text
TicketsController.Create()
        v
return View(new TicketCreateViewModel())
        v
Views/Tickets/Create.cshtml
        v
Kullanici formu doldurur
        v
POST TicketsController.Create(TicketCreateViewModel model)
```

Yani Razor View sadece ekrani gosteren kisim degildir; ayni zamanda form alanlarini ViewModel'e baglayan ve validasyon mesajlarini gosteren katmandir.

## Identity Dosyalari

`Areas/Identity/Pages/Account/` altinda kullanici girisi, kayit ve hesap yonetimi sayfalari bulunur.

Onemli dosyalar:

- `Register.cshtml` ve `Register.cshtml.cs`: Kullanici kaydini yapar. Yeni kayit olan kullaniciya otomatik `Customer` rolu verilir.
- `Login.cshtml` ve `Login.cshtml.cs`: E-posta ve sifre ile giris yapar.
- `Manage/Index.cshtml` ve `Manage/Index.cshtml.cs`: Profil, e-posta ve sifre guncelleme islemlerini yonetir.
- `Manage/ChangePassword.cshtml`, `Manage/Email.cshtml`, `Manage/PersonalData.cshtml`: Hesap yonetimi ekranlarinin parcalaridir.

## Veritabani Baglantisi

Proje Entity Framework Core ile SQL Server'a baglanir.

Baglanti cumlesi `appsettings.json` icindedir:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=TicketFlowDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

Bu ayar sunlari ifade eder:

- `Server=localhost`: Veritabani sunucusu lokal makinedeki SQL Server'dir.
- `Database=TicketFlowDb`: Kullanilacak veritabani adidir.
- `Trusted_Connection=True`: Windows authentication kullanilir.
- `MultipleActiveResultSets=true`: Ayni baglantida birden fazla aktif sorgu sonucuna izin verir.
- `TrustServerCertificate=True`: Gelistirme ortaminda SQL Server sertifika dogrulamasini kolaylastirir.

`Program.cs` icinde baglanti su sekilde okunur:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
```

Ardindan `ApplicationDbContext` SQL Server kullanacak sekilde kaydedilir:

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
```

Yani controller'lar `ApplicationDbContext` istediginde ASP.NET Core dependency injection sistemi bu context'i SQL Server baglantisi ile verir.

## DbContext Yapisi

`Data/ApplicationDbContext.cs`, projenin EF Core context sinifidir:

```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
```

`IdentityDbContext<ApplicationUser>` kalitimi sayesinde standart Identity tablolari da ayni veritabaninda olusur.

Context icinde proje tablolarini temsil eden DbSet'ler:

```csharp
public DbSet<Ticket> Tickets => Set<Ticket>();
public DbSet<TicketReply> TicketReplies => Set<TicketReply>();
public DbSet<SupportCategoryAssignment> SupportCategoryAssignments => Set<SupportCategoryAssignment>();
public DbSet<TicketNotification> TicketNotifications => Set<TicketNotification>();
```

## Veritabani Tablolari

EF Core migration'lara gore ana tablolar sunlardir:

| Tablo | Aciklama |
| --- | --- |
| `AspNetUsers` | Identity kullanicilarini tutar. `ApplicationUser` ile `FullName` alani eklenmistir. |
| `AspNetRoles` | `Customer`, `Support`, `Admin` rollerini tutar. |
| `AspNetUserRoles` | Kullanicilar ile roller arasindaki iliskiyi tutar. |
| `Tickets` | Destek taleplerini tutar. |
| `TicketReplies` | Ticket cevaplarini tutar. |
| `SupportCategoryAssignments` | Support kullanicisi ve kategori yetki eslesmelerini tutar. |
| `TicketNotifications` | Kullanici bildirimlerini tutar. |

Identity ayrica `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens` ve `AspNetRoleClaims` gibi standart tablolar da olusturur.

## Veritabani Iliskileri

`ApplicationDbContext.OnModelCreating()` icinde iliskiler acikca tanimlanir.

### Ticket - Customer Iliskisi

Bir ticket bir musteriye baglidir:

```text
Ticket.CustomerId -> AspNetUsers.Id
```

Silme davranisi:

```text
OnDelete(DeleteBehavior.Restrict)
```

Yani ticket'i olan kullanicinin silinmesi dogrudan cascade olarak ticket'i silmez.

### Ticket - Assigned Support Iliskisi

Bir ticket istege bagli olarak bir support kullanicisina atanabilir:

```text
Ticket.AssignedSupportId -> AspNetUsers.Id
```

Silme davranisi:

```text
OnDelete(DeleteBehavior.SetNull)
```

Yani atanmis support kullanicisi silinirse ticket silinmez, sadece `AssignedSupportId` bosaltilir.

### Ticket - TicketReply Iliskisi

Bir ticket'in birden fazla cevabi olabilir:

```text
Ticket.Id -> TicketReplies.TicketId
```

Silme davranisi:

```text
OnDelete(DeleteBehavior.Cascade)
```

Ticket silinirse ona bagli cevaplar da silinir.

### TicketReply - Author Iliskisi

Her cevap bir kullanici tarafindan yazilir:

```text
TicketReply.AuthorId -> AspNetUsers.Id
```

Silme davranisi `Restrict` olarak ayarlanmistir.

### SupportCategoryAssignments Iliskisi

Support kategori atamalarinda composite primary key kullanilir:

```text
SupportUserId + Category
```

Bu sayede ayni support kullanicisina ayni kategori birden fazla kez atanamaz.

### TicketNotifications Iliskisi

Bildirimler hem kullaniciya hem ticket'a baglidir:

```text
TicketNotification.UserId -> AspNetUsers.Id
TicketNotification.TicketId -> Tickets.Id
```

Iki iliskide de cascade delete vardir. Kullanici veya ticket silindiginde ilgili bildirimler de silinir.

## Migration ve Seed Mantigi

Migration dosyalari `Data/Migrations/` altindadir.

Mevcut migration'lar:

- `20260604080811_InitialSqlServer`: Identity tablolari, `Tickets` ve `TicketReplies` tablolarini olusturur.
- `20260604090810_AddUserFullName`: `AspNetUsers` tablosuna `FullName` alani ekler.
- `20260604182205_AddTicketCategoriesAndSupportAssignments`: Ticket kategorisi ve support kategori yetki tablosunu ekler.
- `20260604192415_AddTicketNotifications`: Bildirim tablosunu ekler.

Uygulama baslarken `Program.cs` icinde su metot calisir:

```csharp
await dbContext.Database.MigrateAsync();
await SeedData.InitializeAsync(services);
```

Bu nedenle uygulama calistiginda:

1. Veritabani yoksa olusturulur.
2. Eksik migration'lar uygulanir.
3. Roller yoksa olusturulur.
4. Demo kullanicilar yoksa eklenir.
5. Varsayilan support kullanicisina tum kategoriler atanir.

## Seed Edilen Demo Kullanicilar

`Data/SeedData.cs` icinde olusturulan demo kullanicilar:

| Rol | Kullanici | E-posta | Sifre |
| --- | --- | --- | --- |
| Customer | `customer` | `customer@ticketflow.local` | `Customer123!` |
| Support | `support` | `support@ticketflow.local` | `Support123!` |
| Admin | `admin` | `admin@ticketflow.local` | `Admin123!` |

## Temel Is Akisi

### Musteri Talep Acar

1. Kullanici kayit olur veya giris yapar.
2. Kayit olan kullaniciya otomatik `Customer` rolu verilir.
3. Musteri `Tickets/Create` ekranindan baslik, kategori ve aciklama girer.
4. `TicketsController.Create()` yeni `Ticket` kaydini olusturur.
5. Talep `Open` durumunda baslar.
6. Ilgili admin ve kategoriye yetkili support kullanicilarina bildirim olusturulur.

### Support Talebi Yonetir

1. Support kullanicisi `Tickets/Index` ekraninda sadece yetkili oldugu kategorileri gorur.
2. Talebi ustlenirse `AssignedSupportId` kendi kullanici id'si olur.
3. Talebe cevap yazabilir.
4. Durumu guncellemek icin once talebi ustlenmis olmasi gerekir.
5. Durum `Open`, `Resolved` veya `Closed` olabilir.

### Admin Yonetim Yapar

1. Admin tum talepleri gorebilir.
2. Musteri arayabilir ve musterinin taleplerini inceleyebilir.
3. Bir kullaniciyi support rolune alabilir.
4. Support kullanicisinin bakabilecegi kategorileri duzenleyebilir.
5. Ticket'a destek sorumlusu atayabilir veya atamayi kaldirabilir.

## Validasyonlar

Form validasyonlari Data Annotations ile yapilir.

Ornekler:

- `Required`
- `StringLength`
- `EmailAddress`
- `Compare`
- `RegularExpression`
- `EnumDataType`

Ticket basligi en fazla 120 karakterdir. Ticket aciklamasi 10-2000 karakter araligindadir. Cevap mesaji 2-1500 karakter araligindadir.

## Calistirma

Projeyi lokal ortamda calistirmak icin:

```powershell
dotnet restore
dotnet tool restore
dotnet run
```

Eger migration'lari elle uygulamak istenirse:

```powershell
dotnet tool run dotnet-ef database update
```

Ancak proje `Program.cs` icinde `MigrateAsync()` kullandigi icin uygulama acilisinda migration'lari otomatik uygulamaya calisir.

`Properties/launchSettings.json` dosyasina gore gelistirme adresleri:

```text
http://localhost:5095
https://localhost:7177
```

## Onemli Not

Proje kokunde `app.db` dosyasi bulunsa da guncel kod `appsettings.json` ve `Program.cs` uzerinden SQL Server kullanir. README dosyasinda SQLite ifadesi geciyorsa bu bilgi eski kalmistir. Guncel veritabani baglantisi:

```text
Server=localhost;Database=TicketFlowDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```
