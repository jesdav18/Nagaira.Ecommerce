import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService } from '../../../core/services/admin.service';

@Component({
  selector: 'app-admin-audit',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-audit.component.html',
  styleUrls: ['./admin-audit.component.css']
})
export class AdminAuditComponent implements OnInit {
  private adminService = inject(AdminService);
  
  auditLogs = signal<any[]>([]);
  loading = signal(true);
  pageNumber = signal(1);
  pageSize = signal(20);

  ngOnInit(): void {
    this.loadAuditLogs();
  }

  loadAuditLogs(): void {
    this.loading.set(true);
    this.adminService.getAuditLogs({
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize()
    }).subscribe({
      next: (response: any) => {
        this.auditLogs.set(response.items || []);
        this.loading.set(false);
      },
      error: (error: any) => {
        console.error('Error loading audit logs:', error);
        this.loading.set(false);
      }
    });
  }
}

