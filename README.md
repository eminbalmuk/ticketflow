# TicketFlow

Teknik destek / ticket sistemi. Müşteriler destek talebi oluşturur; destek ekibi talepleri üstlenir, cevap yazar ve durumlarını günceller.

## Teknik Katmanlar

- ASP.NET Core MVC (.NET 9)
- ASP.NET Core Identity ile authentication
- Customer, Support ve Admin rolleri ile authorization
- EF Core Code-First, SQLite ve migrations
- `TicketStatus` enum tipi: Açık, Çözüldü, Kapandı
- LINQ ile durum ve kullanıcı bazlı ticket filtreleme
- ViewModel ve Data Annotations validasyonları
- Ortak `_Layout.cshtml` header/footer yapısı

## Çalıştırma

```powershell
dotnet restore
dotnet tool restore
dotnet tool run dotnet-ef database update
dotnet run
```

## Demo Kullanıcılar

| Rol | E-posta | Şifre |
| --- | --- | --- |
| Customer | customer@ticketflow.local | Customer123! |
| Support | support@ticketflow.local | Support123! |
| Admin | admin@ticketflow.local | Admin123! |
