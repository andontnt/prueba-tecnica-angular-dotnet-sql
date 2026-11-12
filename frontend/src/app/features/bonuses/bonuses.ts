import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { BonusListItem } from '../../core/models/bonus';
import { MasterStateCodes } from '../../core/models/catalog';
import { PagedResponse } from '../../core/models/paged-response';
import { BonusApi } from '../../core/services/bonus-api';
import { CatalogApi } from '../../core/services/catalog-api';

const PAGE_SIZE = 10;

/**
 * Pantalla de bonos de desempeño: filtros, listado paginado desde el servidor,
 * acciones por fila y modal de registro.
 */
@Component({
  selector: 'app-bonuses',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './bonuses.html',
  styleUrl: './bonuses.css',
})
export class Bonuses {
  private readonly catalogApi = inject(CatalogApi);
  private readonly bonusApi = inject(BonusApi);
  private readonly formBuilder = inject(FormBuilder).nonNullable;

  protected readonly departments = toSignal(this.catalogApi.getDepartments(), { initialValue: [] });
  protected readonly bonusStates = toSignal(this.catalogApi.getStates(MasterStateCodes.Bonus), {
    initialValue: [],
  });

  protected readonly filters = this.formBuilder.group({
    departmentCode: '',
    employeeNumber: this.formBuilder.control<number | null>(null),
    period: '',
    stateCode: '',
  });

  protected readonly bonuses = signal<PagedResponse<BonusListItem> | null>(null);

  constructor() {
    this.search();
  }

  protected search(): void {
    this.loadPage(1);
  }

  protected clear(): void {
    this.filters.reset();
    this.search();
  }

  private loadPage(pageIndex: number): void {
    const { employeeNumber, ...filters } = this.filters.getRawValue();
    this.bonusApi
      .getBonuses({
        ...filters,
        employeeNumber: employeeNumber ?? undefined,
        pageIndex,
        pageSize: PAGE_SIZE,
      })
      .subscribe((page) => this.bonuses.set(page));
  }
}
