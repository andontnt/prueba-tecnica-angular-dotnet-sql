import { DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { filter, Observable, switchMap } from 'rxjs';
import { BonusListItem } from '../../core/models/bonus';
import { BonusStates, BonusTypes, MasterStateCodes } from '../../core/models/catalog';
import { PagedResponse } from '../../core/models/paged-response';
import { BonusApi } from '../../core/services/bonus-api';
import { CatalogApi } from '../../core/services/catalog-api';
import { ConfirmDialog, ConfirmDialogData } from '../../shared/confirm-dialog/confirm-dialog';
import { BonusFormDialog } from './bonus-form-dialog/bonus-form-dialog';

const PAGE_SIZE = 10;

@Component({
  selector: 'app-bonuses',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatPaginatorModule,
    MatSelectModule,
    MatTableModule,
    DecimalPipe,
  ],
  templateUrl: './bonuses.html',
  styleUrl: './bonuses.css',
})
export class Bonuses {
  private readonly catalogApi = inject(CatalogApi);
  private readonly bonusApi = inject(BonusApi);
  private readonly dialog = inject(MatDialog);
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

  protected readonly pageSize = PAGE_SIZE;
  protected readonly bonusTypes = BonusTypes;
  protected readonly registeredState = BonusStates.Registered;
  protected readonly columns = [
    'state',
    'period',
    'employee',
    'employeeName',
    'bonusType',
    'bonusValue',
    'reason',
    'grossAmount',
    'taxAmount',
    'netAmount',
    'actions',
  ];

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

  protected loadPage(pageIndex: number): void {
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

  protected openCreateDialog(): void {
    this.dialog
      .open(BonusFormDialog, { width: '800px', maxWidth: '95vw' })
      .afterClosed()
      .pipe(filter(Boolean))
      .subscribe(() => this.search());
  }

  protected cancel(bonus: BonusListItem): void {
    this.confirmAndRun(
      {
        title: 'Anular bono',
        message: `¿Anular el bono de ${bonus.employeeName} del periodo ${bonus.period}?`,
        confirmText: 'Anular',
      },
      this.bonusApi.cancel(bonus.bonusId),
    );
  }

  protected delete(bonus: BonusListItem): void {
    this.confirmAndRun(
      {
        title: 'Eliminar bono',
        message: `¿Eliminar el bono de ${bonus.employeeName} del periodo ${bonus.period}?`,
        confirmText: 'Eliminar',
      },
      this.bonusApi.delete(bonus.bonusId),
    );
  }

  private confirmAndRun(data: ConfirmDialogData, action: Observable<void>): void {
    this.dialog
      .open(ConfirmDialog, { data })
      .afterClosed()
      .pipe(
        filter(Boolean),
        switchMap(() => action),
      )
      .subscribe(() => this.reloadPage());
  }

  private reloadPage(): void {
    this.loadPage(this.bonuses()?.pageNumber ?? 1);
  }
}
