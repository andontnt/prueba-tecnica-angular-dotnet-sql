import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Employee } from '../../../core/models/bonus';
import { BonusTypes } from '../../../core/models/catalog';
import { BonusApi } from '../../../core/services/bonus-api';
import { CatalogApi } from '../../../core/services/catalog-api';

const round = (value: number) => Math.round((value + Number.EPSILON) * 100) / 100;

const today = new Date();
const currentPeriod = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}`;

@Component({
  selector: 'app-bonus-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    DatePipe,
    DecimalPipe,
  ],
  templateUrl: './bonus-form-dialog.html',
  styleUrl: './bonus-form-dialog.css',
})
export class BonusFormDialog {
  private readonly bonusApi = inject(BonusApi);
  private readonly catalogApi = inject(CatalogApi);
  private readonly dialogRef = inject(MatDialogRef<BonusFormDialog>);
  private readonly formBuilder = inject(FormBuilder).nonNullable;

  protected readonly departments = toSignal(this.catalogApi.getDepartments(), { initialValue: [] });
  protected readonly bonusTypes = toSignal(this.catalogApi.getBonusTypes(), { initialValue: [] });
  protected readonly reasons = toSignal(this.catalogApi.getBonusReasons(), { initialValue: [] });
  protected readonly currentPeriod = currentPeriod;

  protected readonly employee = signal<Employee | null>(null);

  protected readonly searchForm = this.formBuilder.group({
    departmentCode: ['', Validators.required],
    employeeNumber: this.formBuilder.control<number | null>(null, Validators.required),
  });

  protected readonly bonusForm = this.formBuilder.group({
    period: [currentPeriod, Validators.required],
    bonusTypeId: this.formBuilder.control<number | null>(null, Validators.required),
    bonusValue: this.formBuilder.control<number | null>(null, Validators.required),
    reasonCode: ['', Validators.required],
  });

  private readonly bonusValues = toSignal(this.bonusForm.valueChanges, {
    initialValue: this.bonusForm.value,
  });

  protected readonly isPercentage = computed(
    () => this.bonusValues().bonusTypeId === BonusTypes.SalaryPercentage,
  );

  protected readonly maxValue = computed(() =>
    this.isPercentage() ? 100 : (this.employee()?.monthlySalary ?? null),
  );

  protected readonly preview = computed(() => {
    const employee = this.employee();
    const { bonusTypeId, bonusValue } = this.bonusValues();
    if (!employee || !bonusTypeId || !bonusValue) {
      return null;
    }

    const gross = round(
      bonusTypeId === BonusTypes.FixedAmount
        ? bonusValue
        : (employee.monthlySalary * bonusValue) / 100,
    );
    const tax = round((gross * employee.taxRate) / 100);
    return { gross, tax, net: round(gross - tax) };
  });

  protected searchEmployee(): void {
    if (this.searchForm.invalid) {
      this.searchForm.markAllAsTouched();
      return;
    }

    const { departmentCode, employeeNumber } = this.searchForm.getRawValue();
    this.employee.set(null);
    this.bonusApi
      .getEmployee(departmentCode, employeeNumber!)
      .subscribe((employee) => this.employee.set(employee));
  }

  protected save(): void {
    const employee = this.employee();
    if (!employee || this.bonusForm.invalid) {
      this.bonusForm.markAllAsTouched();
      return;
    }

    const { period, bonusTypeId, bonusValue, reasonCode } = this.bonusForm.getRawValue();
    this.bonusApi
      .create({
        departmentCode: employee.departmentCode,
        employeeNumber: employee.employeeNumber,
        period,
        bonusTypeId: bonusTypeId!,
        bonusValue: bonusValue!,
        reasonCode,
      })
      .subscribe(() => this.dialogRef.close(true));
  }
}
