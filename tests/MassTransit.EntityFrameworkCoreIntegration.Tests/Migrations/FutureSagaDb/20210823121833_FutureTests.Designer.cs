﻿// <auto-generated />
using System;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MassTransit.EntityFrameworkCoreIntegration.Tests.Migrations.FutureSagaDb
{
    [DbContext(typeof(FutureSagaDbContext))]
    [Migration("20210823121833_FutureTests")]
    partial class FutureTests
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("MassTransit.Futures.FutureState", b =>
                {
                    b.Property<Guid>("CorrelationId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Command")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("Completed")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("Created")
                        .HasColumnType("datetime2");

                    b.Property<int>("CurrentState")
                        .HasColumnType("int");

                    b.Property<DateTime?>("Faulted")
                        .HasColumnType("datetime2");

                    b.Property<string>("Faults")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Location")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Pending")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Results")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Subscriptions")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Variables")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Version")
                        .HasColumnType("int");

                    b.HasKey("CorrelationId");

                    b.ToTable("FutureState");
                });
#pragma warning restore 612, 618
        }
    }
}
