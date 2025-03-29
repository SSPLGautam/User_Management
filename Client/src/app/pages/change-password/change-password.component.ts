import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ChangePasswordRequest } from '../../interfaces/change-password-request';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-change-password',
  imports: [FormsModule],
  templateUrl: './change-password.component.html',
  styleUrl: './change-password.component.css'
})
export class ChangePasswordComponent {
  currentPassword!: string;
  newPassword!: string;
  matSnackBar = inject(MatSnackBar);
  authService = inject(AuthService);
  router = inject(Router);

  changePasswordHandle() {
    this.authService.changePassword({
      email: this.authService.getUserDetail()?.email,
      currentPassword: this.currentPassword,
      newPassword: this.newPassword
    }).subscribe({
      next: (response) => {
        if (response.isSuccess) {
          this.matSnackBar.open(response.message, 'Close', {
            duration: 3000
          });
          this.authService.logout();
          this.router.navigate(['/login']);
        } else {
          this.matSnackBar.open(response.message, 'Close', {
            duration: 3000
          });
        }
      },
      error: (error: HttpErrorResponse) => {
        this.matSnackBar.open(error.error.message, 'Close', {
          duration: 3000
        });
      }
    });
  }

}
