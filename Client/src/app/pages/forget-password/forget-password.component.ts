import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpErrorResponse } from '@angular/common/http';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-forget-password',
  imports: [FormsModule, MatIconModule],
  templateUrl: './forget-password.component.html',
  styleUrl: './forget-password.component.css'
})
export class ForgetPasswordComponent {
  email!: string;

  authService = inject(AuthService);
  matSnackBar = inject(MatSnackBar);
  showEmailSent = false;
  isSubmitting = false;

  forgotPassword() {
    this.isSubmitting = true;
    this.authService.forgotPassword(this.email).subscribe({
      next: (res) => {
        if (res.isSuccess) {
          this.matSnackBar.open(res.message, "Close", {
            duration: 5000
          });
        } else {
          this.matSnackBar.open(res.message, "Close", {
            duration: 5000
          });
          this.showEmailSent = true
        }
      },
      error: (err: HttpErrorResponse) => {
        this.matSnackBar.open(err.message, "Close", {
          duration: 5000
        });
      },
      complete: () => {
        this.isSubmitting = false
      }
    })
  }
}
