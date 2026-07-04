using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StudentLearningPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KnowledgeBase",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Topic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Keywords = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeBase", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "KnowledgeBase",
                columns: new[] { "Id", "Answer", "Keywords", "Question", "Topic" },
                values: new object[,]
                {
                    { 1, "The registration fee is ₹900 for all programs — B.Tech, BCA, BBA, and MBA.", "fee cost price pay amount money charge registration", "What is the registration fee?", "fee" },
                    { 2, "Greenland University offers: B.Tech (CS, ECE, ME), BCA, BBA, and MBA.", "course program subject offer available study options", "What courses are available?", "course" },
                    { 3, "B.Tech requires minimum 60% in 12th grade with PCM subjects.", "eligibility btech engineering qualify marks percentage criteria 12th pcm", "What is the eligibility for B.Tech?", "eligibility" },
                    { 4, "MBA requires a graduation degree in any stream. Minimum 50% in graduation.", "eligibility mba qualify graduation degree criteria masters", "What is the eligibility for MBA?", "eligibility" },
                    { 5, "BCA requires minimum 55% in 12th grade with Mathematics.", "eligibility bca qualify marks percentage criteria 12th math", "What is the eligibility for BCA?", "eligibility" },
                    { 6, "BBA requires minimum 50% in 12th grade in any stream.", "eligibility bba qualify marks percentage criteria 12th", "What is the eligibility for BBA?", "eligibility" },
                    { 7, "The registration deadline is June 30, 2025. Apply soon to secure your seat!", "deadline last date expire close when registration date time", "What is the registration deadline?", "deadline" },
                    { 8, "B.Tech: 4 years | MBA: 2 years | BCA: 3 years | BBA: 3 years.", "duration year long semester how many years time period", "What is the course duration?", "duration" },
                    { 9, "Required documents: 1) 10th marksheet 2) 12th marksheet 3) Passport photo 4) Aadhaar card.", "document upload certificate paper required needed submit", "What documents are required?", "document" },
                    { 10, "Email: admissions@greenlanduniversity.edu | Office hours: Mon-Fri, 9 AM to 5 PM.", "contact support helpdesk email office phone address reach", "How to contact the university?", "contact" },
                    { 11, "Click the Register tab, fill in your name, email, course, password and other details. Pay ₹900 fee online.", "register signup enroll admission apply join how to create account", "How to register?", "register" },
                    { 12, "Click the Login tab and enter your registered email and password to sign in.", "login signin password forgot account access", "How to login?", "login" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnowledgeBase");
        }
    }
}
