import { Component, inject, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatIcon } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Router, RouterLink } from '@angular/router';
import { RoleService } from '../../services/role.service';
import { Observable } from 'rxjs';
import { Role } from '../../interfaces/role';
import { AsyncPipe, CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpErrorResponse } from '@angular/common/http';
import { ValidationErrors } from '../../interfaces/validation-error';

@Component({
  selector: 'app-register',
  imports: [MatInputModule, MatIcon, MatSelectModule, RouterLink, ReactiveFormsModule, AsyncPipe, CommonModule],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent implements OnInit {
  fb = inject(FormBuilder);
  registerForm!: FormGroup;
  router = inject(Router);

  matSnackBar = inject(MatSnackBar);

  confirmPasswordHide: boolean = true;
  passwordHide: boolean = true;

  roleService = inject(RoleService);
  roles$!: Observable<Role[]>;

  authService = inject(AuthService);

  errors!: ValidationErrors[];

  register() {
    this.authService.register(this.registerForm.value).subscribe({
      next: (response) => {

        this.matSnackBar.open(response.message, 'Close', {
          duration: 5000,
          horizontalPosition: 'center'
        });

        this.router.navigate(['/login']);
      },
      error: (error: HttpErrorResponse) => {
        if (error.status === 400) {
          this.errors = error!.error;
          this.matSnackBar.open(error.error.message, 'Close', {
              duration: 5000,
          horizontalPosition: 'center'
          });
        }
      },
      complete: () => console.log('Resistered successfully')
    });
  }


  ngOnInit() {
    this.registerForm = this.fb.group({
      email: ['', Validators.required, Validators.email],
      password: ['', Validators.required],
      fullName: ['', Validators.required],
      roles: [''],
      confirmPassword: ['', Validators.required]
    },
      {
        validator: this.PasswordMatchValidator
      });
    this.roles$ = this.roleService.getRoles();
  }

  private PasswordMatchValidator(control: AbstractControl): { [key: string]: boolean } | null {
    const password = control.get('password')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;
    if (password !== confirmPassword) return { passwordMismatch: true };

    return null;
  }



}
