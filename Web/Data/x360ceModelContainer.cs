using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#if NETFRAMEWORK
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
#else
using Microsoft.EntityFrameworkCore;
#endif

namespace x360ce.Engine.Data
{
	/// <summary>x360ce Model Container</summary>
	[GeneratedCode("MainModel", "2023.1.19")]
	public partial class x360ceModelContainer : DbContext
	{


		/// <summary>Initialize model</summary>
#if NETFRAMEWORK
		public x360ceModelContainer() : base("name=x360ceModelContainer") { }
#else
		public x360ceModelContainer() : base() { }
#endif

#if NETFRAMEWORK
#else
		/*

		// Set connection string for x360ceModelContainer before calling "var app = builder.Build(); " in Program.cs
		// Requires: using Microsoft.EntityFrameworkCore;
		builder.Services.AddDbContext<x360ceModelContainer>(x =>
		{
			var connectionString = builder.Configuration.GetConnectionString(nameof(x360ceModelContainer) + "Connection");
			x.UseSqlServer(connectionString);
		});
		var db = x360ceModelContainer.Create(connectionString);

		// How to initialize context and retrieve options.
		var options = app.Services.GetService<DbContextOptions<x360ceModelContainer>>();
		var db = new x360ceModelContainer(options)();

		var db = app.Services.GetService<x360ceModelContainer>();

		*/

		/// <summary>Initialize model with options.</summary>
		public x360ceModelContainer(DbContextOptions<x360ceModelContainer> options) : base(options) { }

		/// <summary>Create context with connection string.</summary>
		public static x360ceModelContainer Create(string connectionString) {
			var optionsBuilder = new DbContextOptionsBuilder<x360ceModelContainer>();
			optionsBuilder.UseSqlServer(connectionString);
			return new x360ceModelContainer(optionsBuilder.Options);
		}

		/// <summary>Read the connection string from the application configuration file when no options were given.</summary>
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
			if (!optionsBuilder.IsConfigured)
				optionsBuilder.UseSqlServer(System.Configuration.ConfigurationManager.ConnectionStrings["x360ceModelContainer"].ConnectionString);
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder) {
			modelBuilder.Ignore<Type>();
			modelBuilder.Ignore<System.Reflection.CustomAttributeData>();
			base.OnModelCreating(modelBuilder);

			// Optional hand-authored hook, implemented in a companion partial class
			// (e.g. <Context>.SchemaCompat.cs). A partial void with no implementation
			// is compiled away, so this is harmless for contexts that don't use it.
			OnModelCreatingSchemaCompat(modelBuilder);
		}

		partial void OnModelCreatingSchemaCompat(ModelBuilder modelBuilder);

#endif


		/// <summary>Layout</summary>
		public virtual DbSet<Layout> Layouts { get; set; }

		/// <summary>Pad Setting</summary>
		public virtual DbSet<PadSetting> PadSettings { get; set; }

		/// <summary>Product</summary>
		public virtual DbSet<Product> Products { get; set; }

		/// <summary>Program</summary>
		public virtual DbSet<Program> Programs { get; set; }

		/// <summary>User Setting</summary>
		public virtual DbSet<UserSetting> UserSettings { get; set; }

		/// <summary>Summary</summary>
		public virtual DbSet<Summary> Summaries { get; set; }

		/// <summary>User Computer</summary>
		public virtual DbSet<UserComputer> UserComputers { get; set; }

		/// <summary>User Device</summary>
		public virtual DbSet<UserDevice> UserDevices { get; set; }

		/// <summary>User Game</summary>
		public virtual DbSet<UserGame> UserGames { get; set; }

		/// <summary>User Instance</summary>
		public virtual DbSet<UserInstance> UserInstances { get; set; }

		/// <summary>User Profile</summary>
		public virtual DbSet<UserProfile> UserProfiles { get; set; }

		/// <summary>Vendor</summary>
		public virtual DbSet<Vendor> Vendors { get; set; }

	}
}
