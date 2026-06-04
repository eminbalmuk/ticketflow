# TicketFlow Proje Dokümantasyonu

Bu dosya, TicketFlow projesinin amacını, ASP.NET Core MVC yapısını, temel dosyaların görevlerini ve veritabanı ile nasıl bağlantı kurduğunu açıklar.

## Projenin Amacı

TicketFlow, müşterilerin teknik destek talepleri oluşturabildiği, destek ekibinin bu talepleri takip edip cevaplayabildiği ve yöneticinin destek kullanıcılarını yönetebildiği bir ticket sistemidir.

Proje, ASP.NET Core MVC ve ASP.NET Core Identity üzerine kuruludur. Veriler Entity Framework Core Code First yaklaşımı ile SQL Server veritabanında tutulur.

## Proje Ne Yapar?

- Müşteri kullanıcıları sisteme kayıt olabilir ve giriş yapabilir.
- Müşteriler yeni destek talebi açabilir.
- Talepler başlık, açıklama, kategori, durum, müşteri ve destek sorumlusu bilgileriyle saklanır.
- Müşteri kendi taleplerini listeleyebilir, detaylarını görebilir, cevap yazabilir ve talebini silebilir.
- Support kullanıcıları yetkili oldukları kategorilerdeki talepleri görebilir.
- Support kullanıcıları talebi üstlenebilir, cevap yazabilir ve talep durumunu güncelleyebilir.
- Admin kullanıcısı tüm talepleri görebilir, destek sorumlusu atayabilir, support kullanıcısı oluşturabilir ve support kategori yetkilerini düzenleyebilir.
- Talep açma, cevap yazma, üstlenme, atama ve durum güncelleme gibi işlemlerde ilgili kullanıcılara bildirim oluşturulur.

## Kullanıcı Rolleri

Projede üç temel rol vardır:

| Rol | Açıklama |
| --- | --- |
| `Customer` | Normal müşteri rolüdür. Talep açar, kendi taleplerini görür ve cevap yazar. |
| `Support` | Destek personeli rolüdür. Yetkili olduğu kategorilerdeki talepleri yönetir. |
| `Admin` | Yönetici rolüdür. Tüm talepleri, support kullanıcılarını ve kategori yetkilerini yönetir. |

Bu roller `Data/SeedData.cs` içinde tanımlanır ve uygulama açılışında yoksa otomatik oluşturulur.

## Kullanılan Teknolojiler

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

Paketler `ticketflow.csproj` dosyasında tanımlıdır. Projede özellikle şu paketler kullanılır:

- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Microsoft.AspNetCore.Identity.UI`
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Tools`
- `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore`

## ASP.NET Core Başlangıç Akışı

Uygulamanın ana giriş dosyası `Program.cs` dosyasıdır.

Bu dosyada yapılan başlıca işlemler:

1. `appsettings.json` içinden `DefaultConnection` bağlantı cümlesi okunur.
2. `ApplicationDbContext`, SQL Server kullanacak şekilde servis koleksiyonuna eklenir.
3. ASP.NET Core Identity, `ApplicationUser` sınıfı ile kurulur.
4. Role desteği `AddRoles<IdentityRole>()` ile aktif edilir.
5. MVC controller ve view desteği `AddControllersWithViews()` ile eklenir.
6. Authentication ve authorization middleware'leri aktif edilir.
7. Varsayılan route şu şekilde tanımlanır:

```text
{controller=Home}/{action=Index}/{id?}
```

8. Identity Razor Pages rotaları `MapRazorPages()` ile eklenir.
9. Uygulama başlarken `MigrateAsync()` ile migration'lar veritabanına uygulanır.
10. `SeedData.InitializeAsync()` ile roller ve demo kullanıcılar oluşturulur.

## `Program.cs` Detaylı Açıklama

`Program.cs`, ASP.NET Core uygulamasının başlatıldığı ana dosyadır. Proje çalışmaya başladığında ilk olarak bu dosyadaki kodlar işlenir. Bu yüzden uygulamanın hangi servisleri kullanacağı, veritabanına nasıl bağlanacağı, login/rol sisteminin nasıl kurulacağı ve gelen isteklerin hangi controller'a gideceği burada belirlenir.

Bu projede `Program.cs` iki ana bölüm gibi düşünülebilir:

1. Servislerin tanımlandığı bölüm
2. HTTP request pipeline'in kurulduğu bölüm

### Servislerin Tanımlanması

Servis tanımlama bölümünde uygulamanın kullanacağı altyapı parçaları dependency injection sistemine eklenir.

Veritabanı bağlantısı burada okunur:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
```

Bu satır `appsettings.json` içindeki `DefaultConnection` değerini alır. Eğer bu değer bulunamazsa uygulama hata verir.

Sonra `ApplicationDbContext` SQL Server ile çalışacak şekilde eklenir:

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
```

Bu sayede controller'lar constructor üzerinden `ApplicationDbContext` isteyebilir:

```csharp
public TicketsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
{
    _context = context;
    _userManager = userManager;
}
```

Identity sistemi de yine `Program.cs` içinde kurulur:

```csharp
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
```

Bu kodun anlamı:

- Kullanıcı modeli olarak `ApplicationUser` kullanılır.
- E-posta onayı zorunlu değildir.
- Rol sistemi aktif edilir.
- Identity verileri `ApplicationDbContext` üzerinden veritabanında tutulur.

MVC desteği de şu satırla eklenir:

```csharp
builder.Services.AddControllersWithViews();
```

Bu satır olmadan `Controllers/` ve `Views/` klasörleri MVC mantığıyla çalışmaz.

### Middleware ve Route Yapısı

`Program.cs` içindeki ikinci bölüm gelen HTTP isteklerinin hangi sırayla işleneceğini belirler.

Önemli middleware'ler:

```csharp
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
```

Sırası önemlidir:

- `UseRouting()`: Gelen URL'nin hangi route'a uyduğunu bulur.
- `UseAuthentication()`: Kullanıcının giriş yapıp yapmadığını kontrol eder.
- `UseAuthorization()`: Kullanıcının ilgili sayfaya yetkisi olup olmadığını kontrol eder.

Varsayılan MVC route'u şudur:

```csharp
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

Bu şu anlama gelir:

```text
/Tickets/Details/5
```

isteği geldiğinde ASP.NET Core bunu şu şekilde yorumlar:

```text
Controller: TicketsController
Action: Details
id: 5
```

Identity sayfaları ise Razor Pages olarak çalıştığı için ayrıca map edilir:

```csharp
app.MapRazorPages();
```

Uygulama başlarken veritabanı da hazırlanır:

```csharp
await ApplyMigrationsAndSeedAsync(app.Services);
```

Bu metot içinde:

- `dbContext.Database.MigrateAsync()` ile migration'lar uygulanır.
- `SeedData.InitializeAsync()` ile roller ve demo kullanıcılar oluşturulur.

## ASP.NET Dosya ve Klasör Yapısı

| Dosya/Klasör | Görevi |
| --- | --- |
| `Program.cs` | Uygulamanın servislerini, middleware'lerini, route yapısını, migration ve seed işlemlerini başlatır. |
| `ticketflow.csproj` | Projenin .NET sürümünü ve NuGet paketlerini tanımlar. |
| `appsettings.json` | SQL Server bağlantı cümlesi ve log ayarlarını tutar. |
| `Properties/launchSettings.json` | Local çalıştırma profillerini ve portları tanımlar. |
| `Controllers/` | MVC controller sınıfları bulunur. HTTP isteklerini karşılar ve gerekli view/model sonucunu döndürür. |
| `Models/` | Veritabanına karşılık gelen entity sınıfları, enum'lar ve yardımcı model sınıfları bulunur. |
| `ViewModels/` | View'lara özel veri taşıma sınıfları bulunur. Entity'lerin ekrana doğrudan verilmesini azaltır. |
| `Views/` | Razor view dosyaları bulunur. MVC ekranları burada oluşturulur. |
| `Areas/Identity/` | Login, register ve hesap yönetimi gibi Identity Razor Page dosyaları bulunur. |
| `Data/` | `ApplicationDbContext`, seed verileri ve EF Core migration dosyaları bulunur. |
| `wwwroot/` | CSS, JavaScript, görseller ve frontend kütüphaneleri gibi statik dosyalar bulunur. |
| `publish/` | Yayına alma sonucu üretilmiş derlenmiş çıktıları içerir. Kaynak kodun ana mantığı burada değildir. |

## MVC Akışı

Bu proje ASP.NET Core MVC yapısını kullanır. MVC, `Model`, `View` ve `Controller` parçalarından oluşur.

Genel akış şu şekildedir:

```text
Kullanıcı tarayıcıdan istek gönderir
        v
Program.cs route ayarına göre controller seçilir
        v
Controller action metodu çalışır
        v
Gerekirse ApplicationDbContext ile veritabanına gidilir
        v
ViewModel hazırlanır
        v
Razor View ekrana HTML olarak basılır
```

Örnek:

```text
/Tickets/Create
```

isteği geldiğinde:

1. Route sistemi `TicketsController` sınıfını bulur.
2. `Create()` action'i çalışır.
3. Controller `TicketCreateViewModel` oluşturur.
4. `Views/Tickets/Create.cshtml` dosyası açılır.
5. Kullanıcı formu doldurup gönderince `[HttpPost] Create(...)` action'i çalışır.
6. Veri doğruysa `Ticket` entity'si oluşturulur.
7. `_context.Tickets.Add(ticket)` ile veritabanına eklenir.
8. `SaveChangesAsync()` ile kayıt SQL Server'a yazılır.

## Controller Dosyaları

Controller, kullanıcıdan gelen HTTP isteklerini karşılayan C# sınıfıdır. Controller içindeki public metotlara `action` denir. Her action belirli bir sayfayı açabilir, form verisini işleyebilir, veritabanından veri okuyabilir veya kullanıcıyı başka sayfaya yönlendirebilir.

Bu projede controller'lar `Controllers/` klasöründedir ve genelde şu görevleri yapar:

- Kullanıcının yetkisini kontrol eder.
- Formdan gelen veriyi ViewModel ile alır.
- `ModelState.IsValid` ile form validasyonunu kontrol eder.
- `ApplicationDbContext` ile veritabanından veri okur veya veri yazar.
- Sonucu bir Razor View'a gönderir.
- Gerekirse `RedirectToAction`, `NotFound`, `Forbid` veya `Challenge` sonucu döndürür.

Controller'lar dependency injection ile ihtiyaç duydukları servisleri constructor üzerinden alır.

Örnek:

```csharp
private readonly ApplicationDbContext _context;
private readonly UserManager<ApplicationUser> _userManager;

public TicketsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
{
    _context = context;
    _userManager = userManager;
}
```

Bu örnekte:

- `_context`: Veritabanı işlemleri için kullanılır.
- `_userManager`: Giriş yapan kullanıcı ve rollerle ilgili işlemler için kullanılır.

Controller seviyesinde veya action seviyesinde yetki verilebilir.

Örnek:

```csharp
[Authorize]
public class TicketsController : Controller
```

Bu, `TicketsController` içindeki action'lara sadece giriş yapmış kullanıcıların erişebileceği anlamına gelir.

Admin için daha sınırlı yetki örneği:

```csharp
[Authorize(Roles = SeedData.AdminRole)]
public class AdminController : Controller
```

Bu ise sadece `Admin` rolündeki kullanıcıların admin controller'a erişebileceği anlamına gelir.

### `Controllers/HomeController.cs`

Ana sayfa, gizlilik sayfası ve hata sayfası gibi genel sayfaları yönetir.

Başlıca action'lar:

- `Index()`: Ana sayfayı açar.
- `Privacy()`: Privacy sayfasını açar.
- `Error()`: Hata durumunda hata view modelini döndürür.

### `Controllers/TicketsController.cs`

Projenin ana iş mantığının büyük kısmı bu controller'dadır. Tüm action'lar `[Authorize]` ile korunur, yani giriş yapmayan kullanıcı ticket ekranlarına erişemez.

Başlıca action'lar:

- `Index(TicketStatus? status, bool onlyMine)`: Ticket listesini getirir. Role göre filtreleme yapar.
- `Create()`: Yeni ticket formunu açar.
- `Create(TicketCreateViewModel model)`: Yeni ticket kaydeder.
- `Details(int id)`: Ticket detayını, cevapları ve aksiyonları gösterir.
- `Take(int id)`: Support kullanıcısının talebi üstlenmesini sağlar.
- `AssignSupport(int id, string? supportUserId)`: Admin'in destek sorumlusu atamasını sağlar.
- `Reply(int id, TicketReplyViewModel model)`: Talebe cevap ekler.
- `UpdateStatus(int id, TicketStatus status)`: Talep durumunu günceller.
- `Delete(int id)`: Yetkisi olan kullanıcının talebi silmesini sağlar.

Bu controller ayrıca yetki kontrolü için yardımcı metotlar içerir:

- `CanManageTicketAsync()`
- `CanViewAsync()`
- `CanDeleteAsync()`
- `SupportCanHandleCategoryAsync()`
- `GetAllowedCategoriesAsync()`

Bildirim oluşturma mantığı da `AddTicketNotificationsAsync()` metodu ile burada uygulanır.

### `Controllers/AdminController.cs`

Sadece `Admin` rolündeki kullanıcılar erişebilir:

```csharp
[Authorize(Roles = SeedData.AdminRole)]
```

Başlıca görevleri:

- Admin paneli için özet verileri hazırlar.
- Müşteri arama ve müşterinin ticketlarını gösterme işlemini yapar.
- Var olan veya yeni bir kullanıcıya `Support` rolü verir.
- Support kullanıcılarının bakabileceği ticket kategorilerini düzenler.

### `Controllers/CustomersController.cs`

`Admin` ve `Support` rollerinin müşteri araması yapmasını sağlar:

```csharp
[Authorize(Roles = SeedData.AdminRole + "," + SeedData.SupportRole)]
```

Support kullanıcısı sadece yetkili olduğu kategorilerdeki talepleri görecek şekilde filtrelenir.

### `Controllers/NotificationsController.cs`

Bildirim işlemlerini yönetir.

Başlıca action'lar:

- `Open(int id)`: Bildirimi okundu olarak işaretler ve ilgili ticket detayına yönlendirir.
- `Delete(int id, string? returnUrl)`: Bildirimi siler ve kullanıcıyı önceki sayfaya veya ticket listesine döndürür.

## Model Dosyaları

### `Models/ApplicationUser.cs`

ASP.NET Identity kullanıcısını genişletir:

```csharp
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
```

Standart Identity alanlarına ek olarak `FullName` alanını ekler.

### `Models/Ticket.cs`

Destek talebini temsil eder.

Önemli alanlar:

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

`CustomerId`, talebi açan kullanıcıya bağlanır. `AssignedSupportId`, talebi üstlenen veya admin tarafından atanan destek kullanıcısını tutar.

### `Models/TicketReply.cs`

Ticket üzerindeki cevapları temsil eder.

Önemli alanlar:

- `TicketId`
- `AuthorId`
- `Message`
- `CreatedAt`

Her cevap bir ticket'a ve cevabı yazan kullanıcıya bağlıdır.

### `Models/TicketNotification.cs`

Kullanıcı bildirimlerini temsil eder.

Önemli alanlar:

- `UserId`
- `TicketId`
- `Title`
- `Message`
- `CreatedAt`
- `ReadAt`

`ReadAt` boş ise bildirim okunmamış kabul edilir.

### `Models/SupportCategoryAssignment.cs`

Support kullanıcılarının hangi kategorilerde talep görebileceğini belirler.

Alanlar:

- `SupportUserId`
- `Category`

Bu tabloda bir support kullanıcısı için birden fazla kategori atanabilir.

### Enum Dosyaları

`TicketStatus.cs` destek talebinin durumlarını tutar:

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

`TicketStatusExtensions.cs` ve `TicketCategoryExtensions.cs`, enum değerlerinin ekranda daha okunabilir isimlerle gösterilmesini sağlar.

## ViewModel Dosyaları

ViewModel'ler, controller ile view arasında taşınacak veriyi düzenler. Bu sayede view'lar sadece ihtiyacı olan veriyi alır.

| Dosya | Görevi |
| --- | --- |
| `TicketCreateViewModel.cs` | Ticket oluşturma formundaki başlık, kategori ve açıklama alanlarını taşır. |
| `TicketListViewModel.cs` | Ticket listeleme ekranı için filtreler, sayaçlar ve liste elemanlarını taşır. |
| `TicketDetailsViewModel.cs` | Ticket detay ekranı için ticket bilgisi, cevaplar, yetki bayrakları ve support seçeneklerini taşır. |
| `TicketReplyViewModel.cs` | Talebe cevap yazma formundaki mesaj alanını taşır. |
| `AdminDashboardViewModel.cs` | Admin panelindeki müşteri arama, support kullanıcıları ve özet sayaçları taşır. |
| `CustomerSearchViewModel.cs` | Müşteri arama ekranı ve sonuç ticketlarını taşır. |
| `NotificationMenuItemViewModel.cs` | Header'daki bildirim menüsü için bildirim satırı verisini taşır. |

## View Dosyaları

`Views/` klasörü MVC ekranlarını içerir.

| Dosya/Klasör | Görevi |
| --- | --- |
| `Views/Shared/_Layout.cshtml` | Tüm sayfalarda kullanılan ana HTML iskeleti, navbar, footer, CSS/JS referansları. |
| `Views/Shared/_LoginPartial.cshtml` | Giriş/çıkış alanı, hesap linki ve bildirim kutusunu oluşturur. |
| `Views/Shared/_ValidationScriptsPartial.cshtml` | Client-side form validasyon scriptlerini ekler. |
| `Views/_ViewImports.cshtml` | Razor view'larda ortak `using` ifadelerini ve tag helper'ları tanımlar. |
| `Views/_ViewStart.cshtml` | Varsayılan layout dosyasını belirler. |
| `Views/Home/Index.cshtml` | Ana/landing sayfasıdır. |
| `Views/Tickets/Index.cshtml` | Ticket listeleme ve filtreleme ekranıdır. |
| `Views/Tickets/Create.cshtml` | Yeni ticket oluşturma formudur. |
| `Views/Tickets/Details.cshtml` | Ticket detay, cevaplar, durum güncelleme, üstlenme ve silme ekranıdır. |
| `Views/Admin/Index.cshtml` | Admin paneli, müşteri arama ve support yönetimi ekranıdır. |
| `Views/Admin/EditSupportCategories.cshtml` | Support kullanıcısının kategori yetkilerini düzenleme ekranıdır. |
| `Views/Customers/Index.cshtml` | Admin/support için müşteri arama ekranıdır. |

## Razor View Detaylı Açıklama

Razor View dosyaları `.cshtml` uzantılı dosyalardır. Bu dosyalar HTML ile C# kodunu birlikte kullanarak dinamik sayfalar oluşturur. MVC tarafında controller bir view döndürdüğünde, ilgili `.cshtml` dosyası çalışır ve kullanıcıya HTML olarak gönderilir.

Örnek:

```csharp
return View(model);
```

Bu kod `TicketsController.Create()` içindeyse ASP.NET Core varsayılan olarak şu dosyayı arar:

```text
Views/Tickets/Create.cshtml
```

### Razor View İçindeki `@model`

Bir view dosyasının başında genelde hangi ViewModel'i kullanacağı yazılır.

Örnek:

```csharp
@model TicketCreateViewModel
```

Bu, view içinde `Model` nesnesinin `TicketCreateViewModel` tipinde olduğu anlamına gelir.

Bu sayede view içinde şu alanlara erişilebilir:

```csharp
Model.Title
Model.Category
Model.Description
```

### Tag Helper Kullanımı

Razor View'larda ASP.NET Core tag helper'ları kullanılır. Tag helper'lar HTML elemanlarını controller, action ve model alanlarıyla bağlamayı kolaylaştırır.

Örnek:

```html
<form asp-action="Create" method="post">
```

Bu form submit edildiğinde aynı controller içindeki `Create` action'ina gider.

Input örneği:

```html
<input asp-for="Title" class="form-control" />
```

Bu kod `TicketCreateViewModel.Title` alanına bağlı bir input oluşturur.

Validasyon mesajı örneği:

```html
<span asp-validation-for="Title" class="text-danger"></span>
```

Eğer `Title` alanı boş bırakılırsa veya kurallara uymazsa hata mesajı burada gösterilir.

### Layout ve Partial View

`Views/_ViewStart.cshtml` dosyası varsayılan layout'u belirler:

```csharp
Layout = "_Layout";
```

Bu nedenle sayfalar genel olarak `Views/Shared/_Layout.cshtml` içindeki ortak tasarım içinde gösterilir. Navbar, footer, CSS ve JavaScript referansları burada bulunur.

Partial view ise bir sayfanın tekrar kullanılabilir parçasıdır.

Örnek:

```csharp
<partial name="_LoginPartial" />
```

Bu kod `Views/Shared/_LoginPartial.cshtml` dosyasını layout içine ekler. Bu projede login/çıkış linkleri ve bildirim kutusu bu partial içindedir.

### Script Section

Bazi view dosyaları sayfaya özel script eklemek için `Scripts` section'i kullanır.

Örnek:

```csharp
@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

Bu kod client-side form validasyon scriptlerini sayfaya ekler.

### Bu Projede Razor View Akışı

Ticket oluşturma ekranı üzerinden örnek:

```text
TicketsController.Create()
        v
return View(new TicketCreateViewModel())
        v
Views/Tickets/Create.cshtml
        v
Kullanıcı formu doldurur
        v
POST TicketsController.Create(TicketCreateViewModel model)
```

Yani Razor View sadece ekranı gösteren kısım değildir; aynı zamanda form alanlarını ViewModel'e bağlayan ve validasyon mesajlarını gösteren katmandır.

## Identity Dosyaları

`Areas/Identity/Pages/Account/` altında kullanıcı girişi, kayıt ve hesap yönetimi sayfaları bulunur.

Önemli dosyalar:

- `Register.cshtml` ve `Register.cshtml.cs`: Kullanıcı kaydını yapar. Yeni kayıt olan kullanıcıya otomatik `Customer` rolü verilir.
- `Login.cshtml` ve `Login.cshtml.cs`: E-posta ve şifre ile giriş yapar.
- `Manage/Index.cshtml` ve `Manage/Index.cshtml.cs`: Profil, e-posta ve şifre güncelleme işlemlerini yönetir.
- `Manage/ChangePassword.cshtml`, `Manage/Email.cshtml`, `Manage/PersonalData.cshtml`: Hesap yönetimi ekranlarinin parçalarıdır.

## Veritabanı Bağlantısı

Proje Entity Framework Core ile SQL Server'a bağlanır.

Bağlantı cümlesi `appsettings.json` içindedir:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=TicketFlowDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

Bu ayar şunları ifade eder:

- `Server=localhost`: Veritabanı sunucusu lokal makinedeki SQL Server'dir.
- `Database=TicketFlowDb`: Kullanilacak veritabanı adıdır.
- `Trusted_Connection=True`: Windows authentication kullanılır.
- `MultipleActiveResultSets=true`: Aynı bağlantıda birden fazla aktif sorgu sonucuna izin verir.
- `TrustServerCertificate=True`: Geliştirme ortamında SQL Server sertifika doğrulamasını kolaylaştırır.

`Program.cs` içinde bağlantı şu şekilde okunur:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
```

Ardından `ApplicationDbContext` SQL Server kullanacak şekilde kaydedilir:

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
```

Yani controller'lar `ApplicationDbContext` istediğinde ASP.NET Core dependency injection sistemi bu context'i SQL Server bağlantısı ile verir.

## DbContext Yapısı

`Data/ApplicationDbContext.cs`, projenin EF Core context sınıfıdır:

```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
```

`IdentityDbContext<ApplicationUser>` kalıtımı sayesinde standart Identity tabloları da aynı veritabanında oluşur.

Context içinde proje tablolarını temsil eden DbSet'ler:

```csharp
public DbSet<Ticket> Tickets => Set<Ticket>();
public DbSet<TicketReply> TicketReplies => Set<TicketReply>();
public DbSet<SupportCategoryAssignment> SupportCategoryAssignments => Set<SupportCategoryAssignment>();
public DbSet<TicketNotification> TicketNotifications => Set<TicketNotification>();
```

## Veritabanı Tabloları

EF Core migration'lara göre ana tablolar şunlardır:

| Tablo | Açıklama |
| --- | --- |
| `AspNetUsers` | Identity kullanıcılarını tutar. `ApplicationUser` ile `FullName` alanı eklenmiştir. |
| `AspNetRoles` | `Customer`, `Support`, `Admin` rollerini tutar. |
| `AspNetUserRoles` | Kullanıcılar ile roller arasındaki ilişkiyi tutar. |
| `Tickets` | Destek taleplerini tutar. |
| `TicketReplies` | Ticket cevaplarını tutar. |
| `SupportCategoryAssignments` | Support kullanıcısı ve kategori yetki eşleşmelerini tutar. |
| `TicketNotifications` | Kullanıcı bildirimlerini tutar. |

Identity ayrıca `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens` ve `AspNetRoleClaims` gibi standart tablolar da oluşturur.

## Veritabanı İlişkileri

`ApplicationDbContext.OnModelCreating()` içinde ilişkiler açıkça tanımlanır.

### Ticket - Customer İlişkisi

Bir ticket bir müşteriye bağlıdır:

```text
Ticket.CustomerId -> AspNetUsers.Id
```

Silme davranışı:

```text
OnDelete(DeleteBehavior.Restrict)
```

Yani ticket'i olan kullanıcının silinmesi doğrudan cascade olarak ticket'i silmez.

### Ticket - Assigned Support İlişkisi

Bir ticket isteğe bağlı olarak bir support kullanıcısına atanabilir:

```text
Ticket.AssignedSupportId -> AspNetUsers.Id
```

Silme davranışı:

```text
OnDelete(DeleteBehavior.SetNull)
```

Yani atanmış support kullanıcısı silinirse ticket silinmez, sadece `AssignedSupportId` boşaltılır.

### Ticket - TicketReply İlişkisi

Bir ticket'in birden fazla cevabı olabilir:

```text
Ticket.Id -> TicketReplies.TicketId
```

Silme davranışı:

```text
OnDelete(DeleteBehavior.Cascade)
```

Ticket silinirse ona bağlı cevaplar da silinir.

### TicketReply - Author İlişkisi

Her cevap bir kullanıcı tarafından yazılır:

```text
TicketReply.AuthorId -> AspNetUsers.Id
```

Silme davranışı `Restrict` olarak ayarlanmıştır.

### SupportCategoryAssignments İlişkisi

Support kategori atamalarında composite primary key kullanılır:

```text
SupportUserId + Category
```

Bu sayede aynı support kullanıcısına aynı kategori birden fazla kez atanamaz.

### TicketNotifications İlişkisi

Bildirimler hem kullanıcıya hem ticket'a bağlıdır:

```text
TicketNotification.UserId -> AspNetUsers.Id
TicketNotification.TicketId -> Tickets.Id
```

İki ilişkide de cascade delete vardır. Kullanıcı veya ticket silindiğinde ilgili bildirimler de silinir.

## Migration ve Seed Mantığı

Migration dosyaları `Data/Migrations/` altındadır.

Mevcut migration'lar:

- `20260604080811_InitialSqlServer`: Identity tabloları, `Tickets` ve `TicketReplies` tablolarını oluşturur.
- `20260604090810_AddUserFullName`: `AspNetUsers` tablosuna `FullName` alanı ekler.
- `20260604182205_AddTicketCategoriesAndSupportAssignments`: Ticket kategorisi ve support kategori yetki tablosunu ekler.
- `20260604192415_AddTicketNotifications`: Bildirim tablosunu ekler.

Uygulama başlarken `Program.cs` içinde şu metot çalışır:

```csharp
await dbContext.Database.MigrateAsync();
await SeedData.InitializeAsync(services);
```

Bu nedenle uygulama çalıştığında:

1. Veritabanı yoksa oluşturulur.
2. Eksik migration'lar uygulanır.
3. Roller yoksa oluşturulur.
4. Demo kullanıcılar yoksa eklenir.
5. Varsayılan support kullanıcısına tüm kategoriler atanır.

## Seed Edilen Demo Kullanıcılar

`Data/SeedData.cs` içinde oluşturulan demo kullanıcılar:

| Rol | Kullanıcı | E-posta | Şifre |
| --- | --- | --- | --- |
| Customer | `customer` | `customer@ticketflow.local` | `Customer123!` |
| Support | `support` | `support@ticketflow.local` | `Support123!` |
| Admin | `admin` | `admin@ticketflow.local` | `Admin123!` |

## Temel İş Akışı

### Müşteri Talep Acar

1. Kullanıcı kayıt olur veya giriş yapar.
2. Kayıt olan kullanıcıya otomatik `Customer` rolü verilir.
3. Müşteri `Tickets/Create` ekranından başlık, kategori ve açıklama girer.
4. `TicketsController.Create()` yeni `Ticket` kaydını oluşturur.
5. Talep `Open` durumunda başlar.
6. İlgili admin ve kategoriye yetkili support kullanıcılarına bildirim oluşturulur.

### Support Talebi Yönetir

1. Support kullanıcısı `Tickets/Index` ekraninda sadece yetkili olduğu kategorileri görür.
2. Talebi üstlenirse `AssignedSupportId` kendi kullanıcı id'si olur.
3. Talebe cevap yazabilir.
4. Durumu güncellemek için önce talebi üstlenmiş olması gerekir.
5. Durum `Open`, `Resolved` veya `Closed` olabilir.

### Admin Yönetim Yapar

1. Admin tüm talepleri görebilir.
2. Müşteri arayabilir ve müşterinin taleplerini inceleyebilir.
3. Bir kullanıcıyı support rolüne alabilir.
4. Support kullanıcısının bakabileceği kategorileri düzenleyebilir.
5. Ticket'a destek sorumlusu atayabilir veya atamayı kaldırabilir.

## Validasyonlar

Form validasyonları Data Annotations ile yapılır.

Örnekler:

- `Required`
- `StringLength`
- `EmailAddress`
- `Compare`
- `RegularExpression`
- `EnumDataType`

Ticket başlığı en fazla 120 karakterdir. Ticket açıklaması 10-2000 karakter aralığındadır. Cevap mesajı 2-1500 karakter aralığındadır.

## Çalıştırma

Projeyi lokal ortamda çalıştırmak için:

```powershell
dotnet restore
dotnet tool restore
dotnet run
```

Eğer migration'ları elle uygulamak istenirse:

```powershell
dotnet tool run dotnet-ef database update
```

Ancak proje `Program.cs` içinde `MigrateAsync()` kullandığı için uygulama açılışında migration'ları otomatik uygulamaya çalışır.

`Properties/launchSettings.json` dosyasına göre geliştirme adresleri:

```text
http://localhost:5095
https://localhost:7177
```

## Önemli Not

Proje kökünde `app.db` dosyası bulunsa da güncel kod `appsettings.json` ve `Program.cs` üzerinden SQL Server kullanır. README dosyasında SQLite ifadesi geçiyorsa bu bilgi eski kalmıştır. Güncel veritabanı bağlantısı:

```text
Server=localhost;Database=TicketFlowDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```
