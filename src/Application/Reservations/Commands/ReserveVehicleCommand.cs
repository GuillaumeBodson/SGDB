﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Reservations.Commands;
public class ReserveVehicleCommand: IRequest<bool>
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public PlanType PlanType { get; set; }
    public int VehicleId { get; set; }
    public int StartDepotId { get; set; }
    public int? EndDepotId { get; set; }
    public int ClientId { get; set; }
}
public class ReserveVehicleCommandHandler : IRequestHandler<ReserveVehicleCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTime _dateTime;
    private readonly IPriceCalculationService _priceCalculation;

    public ReserveVehicleCommandHandler(IApplicationDbContext context, IDateTime dateTime, IPriceCalculationService priceCalculation)
    {
        _context = context;
        _dateTime = dateTime;
        _priceCalculation = priceCalculation;
    }
    public async Task<bool> Handle(ReserveVehicleCommand request, CancellationToken cancellationToken)
    {
        var startDepot = await _context.Depots.Include(d => d.Country).FirstOrDefaultAsync(d => d.Id == request.StartDepotId) ?? throw new NotFoundException(nameof(request.StartDepotId));
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(d => d.Id == request.VehicleId) ?? throw new NotFoundException(nameof(request.VehicleId));
        var client = await _context.Clients.FirstOrDefaultAsync(d => d.Id == request.ClientId) ?? throw new NotFoundException(nameof(request.ClientId));

        Reservation reservation = new() { StartDate = request.StartDate, EndDate = request.EndDate, Vehicle = vehicle, Client = client };
        var bonus = _priceCalculation.CalculateBonus(request.StartDate);

        Plan plan = new() { StartDepot = startDepot, PlanType = request.PlanType, Reservation = reservation, BonusRate = bonus };

        if(request.EndDepotId.HasValue && request.EndDepotId.Value > 0)
            plan.EndDepot = await _context.Depots.FirstOrDefaultAsync(d => d.Id == request.EndDepotId.Value) ?? throw new NotFoundException(nameof(request.EndDepotId));

        if (request.PlanType == PlanType.Kilometric)
            plan.KilometerPrice = startDepot.Country.KilometerPrice;

        reservation.Plan = plan;
        reservation.ReservationStatus = request.StartDate.Date == DateTime.Now.Date ? ReservationStatus.Pending : ReservationStatus.Booked;

        _context.Reservations.Add(reservation);
        return await _context.SaveChangesAsync(cancellationToken) > 0;
    }
}
