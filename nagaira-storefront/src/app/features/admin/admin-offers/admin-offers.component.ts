import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService } from '../../../core/services/admin.service';

@Component({
  selector: 'app-admin-offers',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-offers.component.html',
  styleUrls: ['./admin-offers.component.css']
})
export class AdminOffersComponent implements OnInit {
  private adminService = inject(AdminService);
  
  offers = signal<any[]>([]);
  loading = signal(true);

  ngOnInit(): void {
    this.loadOffers();
  }

  loadOffers(): void {
    this.loading.set(true);
    this.adminService.getAllOffers().subscribe({
      next: (offers: any) => {
        this.offers.set(offers);
        this.loading.set(false);
      },
      error: (error: any) => {
        console.error('Error loading offers:', error);
        this.loading.set(false);
      }
    });
  }

  toggleOfferStatus(offer: any): void {
    const action = offer.isActive 
      ? this.adminService.deactivateOffer(offer.id)
      : this.adminService.activateOffer(offer.id);
    
    action.subscribe({
      next: () => {
        this.loadOffers();
      },
      error: (error: any) => {
        console.error('Error updating offer:', error);
        alert('Error al actualizar la oferta');
      }
    });
  }
}

