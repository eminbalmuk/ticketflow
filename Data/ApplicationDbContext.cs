using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ticketflow.Models;

namespace ticketflow.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Ticket> Tickets => Set<Ticket>();

    public DbSet<TicketReply> TicketReplies => Set<TicketReply>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Ticket>()
            .HasOne(ticket => ticket.Customer)
            .WithMany()
            .HasForeignKey(ticket => ticket.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Ticket>()
            .HasOne(ticket => ticket.AssignedSupport)
            .WithMany()
            .HasForeignKey(ticket => ticket.AssignedSupportId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<TicketReply>()
            .HasOne(reply => reply.Ticket)
            .WithMany(ticket => ticket.Replies)
            .HasForeignKey(reply => reply.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TicketReply>()
            .HasOne(reply => reply.Author)
            .WithMany()
            .HasForeignKey(reply => reply.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
