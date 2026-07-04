using Microsoft.EntityFrameworkCore;
using StudentLearningPortal.Models;

namespace StudentLearningPortal.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<KnowledgeBase> KnowledgeBase => Set<KnowledgeBase>();
    public DbSet<StudentDocument> StudentDocuments => Set<StudentDocument>();
    public DbSet<ChatLog> ChatLogs => Set<ChatLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>()
            .HasIndex(s => s.Email)
            .IsUnique();

        // Seed university knowledge base data
        modelBuilder.Entity<KnowledgeBase>().HasData(
            new KnowledgeBase { Id = 1, Topic = "fee", Question = "What is the registration fee?", Answer = "The registration fee is ₹900 for all programs — B.Tech, BCA, BBA, and MBA.", Keywords = "fee cost price pay amount money charge registration" },
            new KnowledgeBase { Id = 2, Topic = "course", Question = "What courses are available?", Answer = "Greenland University offers: B.Tech (CS, ECE, ME), BCA, BBA, and MBA.", Keywords = "course program subject offer available study options" },
            new KnowledgeBase { Id = 3, Topic = "eligibility", Question = "What is the eligibility for B.Tech?", Answer = "B.Tech requires minimum 60% in 12th grade with PCM subjects.", Keywords = "eligibility btech engineering qualify marks percentage criteria 12th pcm" },
            new KnowledgeBase { Id = 4, Topic = "eligibility", Question = "What is the eligibility for MBA?", Answer = "MBA requires a graduation degree in any stream. Minimum 50% in graduation.", Keywords = "eligibility mba qualify graduation degree criteria masters" },
            new KnowledgeBase { Id = 5, Topic = "eligibility", Question = "What is the eligibility for BCA?", Answer = "BCA requires minimum 55% in 12th grade with Mathematics.", Keywords = "eligibility bca qualify marks percentage criteria 12th math" },
            new KnowledgeBase { Id = 6, Topic = "eligibility", Question = "What is the eligibility for BBA?", Answer = "BBA requires minimum 50% in 12th grade in any stream.", Keywords = "eligibility bba qualify marks percentage criteria 12th" },
            new KnowledgeBase { Id = 7, Topic = "deadline", Question = "What is the registration deadline?", Answer = "The registration deadline is June 30, 2025. Apply soon to secure your seat!", Keywords = "deadline last date expire close when registration date time" },
            new KnowledgeBase { Id = 8, Topic = "duration", Question = "What is the course duration?", Answer = "B.Tech: 4 years | MBA: 2 years | BCA: 3 years | BBA: 3 years.", Keywords = "duration year long semester how many years time period" },
            new KnowledgeBase { Id = 9, Topic = "document", Question = "What documents are required?", Answer = "Required documents: 1) 10th marksheet 2) 12th marksheet 3) Passport photo 4) Aadhaar card.", Keywords = "document upload certificate paper required needed submit" },
            new KnowledgeBase { Id = 10, Topic = "contact", Question = "How to contact the university?", Answer = "Email: admissions@greenlanduniversity.edu | Office hours: Mon-Fri, 9 AM to 5 PM.", Keywords = "contact support helpdesk email office phone address reach" },
            new KnowledgeBase { Id = 11, Topic = "register", Question = "How to register?", Answer = "Click the Register tab, fill in your name, email, course, password and other details. Pay ₹900 fee online.", Keywords = "register signup enroll admission apply join how to create account" },
            new KnowledgeBase { Id = 12, Topic = "login", Question = "How to login?", Answer = "Click the Login tab and enter your registered email and password to sign in.", Keywords = "login signin password forgot account access" }
        );
    }
}
