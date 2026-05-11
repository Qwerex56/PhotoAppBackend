using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediaService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "media");

            migrationBuilder.CreateTable(
                name: "albums",
                schema: "media",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_albums", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "media_tags",
                schema: "media",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "album_shares",
                schema: "media",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlbumId = table.Column<Guid>(type: "uuid", nullable: false),
                    SharedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SharedWithUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Permission = table.Column<int>(type: "integer", nullable: false),
                    SharedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_album_shares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_album_shares_albums_AlbumId",
                        column: x => x.AlbumId,
                        principalSchema: "media",
                        principalTable: "albums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "media_assets",
                schema: "media",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlbumId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Checksum = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    SafetyStatus = table.Column<int>(type: "integer", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_media_assets_albums_AlbumId",
                        column: x => x.AlbumId,
                        principalSchema: "media",
                        principalTable: "albums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "media_shares",
                schema: "media",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    SharedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SharedWithUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Permission = table.Column<int>(type: "integer", nullable: false),
                    SharedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_shares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_media_shares_media_assets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalSchema: "media",
                        principalTable: "media_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "media_tag_assignments",
                schema: "media",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaTagId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_tag_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_media_tag_assignments_media_assets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalSchema: "media",
                        principalTable: "media_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_media_tag_assignments_media_tags_MediaTagId",
                        column: x => x.MediaTagId,
                        principalSchema: "media",
                        principalTable: "media_tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_album_shares_AlbumId_SharedWithUserId",
                schema: "media",
                table: "album_shares",
                columns: new[] { "AlbumId", "SharedWithUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_album_shares_SharedWithUserId",
                schema: "media",
                table: "album_shares",
                column: "SharedWithUserId");

            migrationBuilder.CreateIndex(
                name: "IX_albums_OwnerId",
                schema: "media",
                table: "albums",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_albums_OwnerId_Name",
                schema: "media",
                table: "albums",
                columns: new[] { "OwnerId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_media_assets_AlbumId",
                schema: "media",
                table: "media_assets",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_media_assets_AlbumId_DisplayName",
                schema: "media",
                table: "media_assets",
                columns: new[] { "AlbumId", "DisplayName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_media_assets_OwnerId",
                schema: "media",
                table: "media_assets",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_media_shares_MediaAssetId_SharedWithUserId",
                schema: "media",
                table: "media_shares",
                columns: new[] { "MediaAssetId", "SharedWithUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_media_shares_SharedWithUserId",
                schema: "media",
                table: "media_shares",
                column: "SharedWithUserId");

            migrationBuilder.CreateIndex(
                name: "IX_media_tag_assignments_MediaAssetId",
                schema: "media",
                table: "media_tag_assignments",
                column: "MediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_media_tag_assignments_MediaAssetId_MediaTagId",
                schema: "media",
                table: "media_tag_assignments",
                columns: new[] { "MediaAssetId", "MediaTagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_media_tag_assignments_MediaTagId",
                schema: "media",
                table: "media_tag_assignments",
                column: "MediaTagId");

            migrationBuilder.CreateIndex(
                name: "IX_media_tags_OwnerId",
                schema: "media",
                table: "media_tags",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_media_tags_OwnerId_Name",
                schema: "media",
                table: "media_tags",
                columns: new[] { "OwnerId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "album_shares",
                schema: "media");

            migrationBuilder.DropTable(
                name: "media_shares",
                schema: "media");

            migrationBuilder.DropTable(
                name: "media_tag_assignments",
                schema: "media");

            migrationBuilder.DropTable(
                name: "media_assets",
                schema: "media");

            migrationBuilder.DropTable(
                name: "media_tags",
                schema: "media");

            migrationBuilder.DropTable(
                name: "albums",
                schema: "media");
        }
    }
}
