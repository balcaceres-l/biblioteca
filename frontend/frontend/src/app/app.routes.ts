import { Routes } from '@angular/router';
import { LoginComponent } from './pages/login/login.component';
import { RegisterComponent } from './pages/register/register.component';
import { LibrosComponent } from './pages/libros/libros.component';
import { AutoresComponent } from './pages/autores/autores.component';
import { CategoriasComponent } from './pages/categorias/categorias.component';
import { LectoresComponent } from './pages/lectores/lectores.component';
import { PrestamosComponent } from './pages/prestamos/prestamos.component';
import { authGuard, roleGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'libros', component: LibrosComponent, canActivate: [authGuard] },
  { path: 'autores', component: AutoresComponent, canActivate: [authGuard] },
  { path: 'categorias', component: CategoriasComponent, canActivate: [authGuard] },
  {
    path: 'lectores',
    component: LectoresComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Administrador', 'Bibliotecario'] }
  },
  { path: 'prestamos', component: PrestamosComponent, canActivate: [authGuard] },
  { path: '', redirectTo: '/libros', pathMatch: 'full' },
  { path: '**', redirectTo: '/libros' }
];
