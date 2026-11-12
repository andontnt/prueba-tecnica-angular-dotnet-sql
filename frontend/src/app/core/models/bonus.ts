export interface Employee {
  departmentCode: string;
  employeeNumber: number;
  fullName: string;
  hireDate: string;
  monthlySalary: number;
  taxRate: number;
}

export interface CreateBonusRequest {
  departmentCode: string;
  employeeNumber: number;
  period: string;
  bonusTypeId: number;
  bonusValue: number;
  reasonCode: string;
}

export interface BonusListItem {
  bonusId: number;
  bonusStateCode: string;
  stateDescription: string;
  period: string;
  departmentCode: string;
  departmentName: string;
  employeeNumber: number;
  employeeName: string;
  bonusTypeId: number;
  bonusTypeDescription: string;
  bonusValue: number;
  reasonDescription: string;
  grossAmount: number;
  taxAmount: number;
  netAmount: number;
}

export interface BonusFilter {
  departmentCode?: string;
  employeeNumber?: number;
  period?: string;
  stateCode?: string;
  pageIndex: number;
  pageSize: number;
}
