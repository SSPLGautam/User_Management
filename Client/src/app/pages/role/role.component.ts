import { Component, inject } from '@angular/core';
import { RoleFormComponent } from '../../components/role-form/role-form.component';
import { RoleService } from '../../services/role.service';
import { RoleCreateRequest } from '../../interfaces/role-create-request';
import { HttpErrorResponse } from '@angular/common/http';
import { RoleListComponent } from '../../components/role-list/role-list.component';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AsyncPipe } from '@angular/common';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-role',
  imports: [RoleFormComponent, RoleListComponent,MatSelectModule,MatInputModule, MatSnackBarModule, AsyncPipe],
  templateUrl: './role.component.html',
  styleUrl: './role.component.css'
})
export class RoleComponent {
  roleService = inject(RoleService);
  authService = inject(AuthService);
  matSnackBar = inject(MatSnackBar);
  errorMessage = '';
  role: RoleCreateRequest = {} as RoleCreateRequest;
  roles$ = this.roleService.getRoles();
  users$ = this.authService.getAll();
  selectedUser:string = '';
  selectedRole:string = '';

  createRole(role: RoleCreateRequest) {
    this.roleService.createRole(role).subscribe({
      next: () => {
        this.roles$ = this.roleService.getRoles();
        this.matSnackBar.open('Role created successfully', 'Ok', {
          duration: 3000
        });
      },
      error: (error: HttpErrorResponse) => {
        if (error.status === 400) {
          this.errorMessage = error.error;
        }
      }
    })
  }

  deleteRole(id: string) {
    this.roleService.deleteRole(id).subscribe({
      next: (response: { message: string }) => {
        this.roles$ = this.roleService.getRoles();
        this.matSnackBar.open('Role deleted successfully', 'Close', {
          duration: 3000
        });
      },
      error: (error: HttpErrorResponse) => {
        this.matSnackBar.open(error.message, 'Close', {
          duration: 3000
        });
      }
    })
  }
  assignRole(){
    this.roleService.assignRole(this.selectedUser, this.selectedRole).subscribe({
      next: (response: { message: string }) => {
        this.roles$ = this.roleService.getRoles();
        this.matSnackBar.open('Role assigned successfully', 'Close', {
          duration: 3000
        });
      },
      error: (error: HttpErrorResponse) => {
        this.matSnackBar.open(error.message, 'Close', {
          duration: 3000
        });
      }
    })
  }


}
