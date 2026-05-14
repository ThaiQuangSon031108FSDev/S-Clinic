using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SClinic.Data;
using SClinic.Models;
using SClinic.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SClinic.Tests;

public class InvoiceServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly SqliteConnection _connection;
    private readonly InvoiceService _service;

    public InvoiceServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ApplicationDbContext(options);
        _db.Database.EnsureCreated();

        _service = new InvoiceService(_db);
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
        _db.Dispose();
    }

    [Fact]
    public async Task PayInvoiceAsync_ShouldDeductStock_WhenSufficientStock()
    {
        // Arrange
        var category = new Category { CategoryName = "Painkillers", Description = "A" };
        _db.Categories.Add(category);

        var medicine = new Medicine { MedicineName = "Paracetamol", Category = category, StockQuantity = 10, Price = 10 };
        _db.Medicines.Add(medicine);
        await _db.SaveChangesAsync();

        var invoice = new Invoice 
        { 
            PaymentStatus = PaymentStatus.Pending,
            CreatedDate = DateTime.Now,
            InvoiceDetails = new List<InvoiceDetail>
            {
                new InvoiceDetail { ItemType = InvoiceItemType.Medicine, MedicineId = medicine.MedicineId, Quantity = 3, UnitPrice = 10, SubTotal = 30 }
            }
        };
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.PayInvoiceAsync(invoice.InvoiceId);

        // Assert
        Assert.True(result);
        var updatedInvoice = await _db.Invoices.FindAsync(invoice.InvoiceId);
        Assert.Equal(PaymentStatus.Paid, updatedInvoice!.PaymentStatus);

        var updatedMedicine = await _db.Medicines.FindAsync(medicine.MedicineId);
        Assert.Equal(7, updatedMedicine!.StockQuantity);
    }

    [Fact]
    public async Task PayInvoiceAsync_ShouldThrowException_WhenInsufficientStock()
    {
        // Arrange
        var category = new Category { CategoryName = "Painkillers", Description = "A" };
        _db.Categories.Add(category);

        var medicine = new Medicine { MedicineName = "Aspirin", Category = category, StockQuantity = 2, Price = 10 };
        _db.Medicines.Add(medicine);
        await _db.SaveChangesAsync();

        var invoice = new Invoice 
        { 
            PaymentStatus = PaymentStatus.Pending,
            CreatedDate = DateTime.Now,
            InvoiceDetails = new List<InvoiceDetail>
            {
                new InvoiceDetail { ItemType = InvoiceItemType.Medicine, MedicineId = medicine.MedicineId, Quantity = 5, UnitPrice = 10, SubTotal = 50 }
            }
        };
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.PayInvoiceAsync(invoice.InvoiceId));
        Assert.Contains("Insufficient stock", ex.Message);
        
        // Ensure not marked as paid
        var updatedInvoice = await _db.Invoices.FindAsync(invoice.InvoiceId);
        Assert.Equal(PaymentStatus.Pending, updatedInvoice!.PaymentStatus);

        // Ensure stock not deducted
        var updatedMedicine = await _db.Medicines.FindAsync(medicine.MedicineId);
        Assert.Equal(2, updatedMedicine!.StockQuantity);
    }
}
