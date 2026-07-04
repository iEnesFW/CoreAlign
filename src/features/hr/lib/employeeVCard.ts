import type { Employee } from '../model/employee.types';

const escapeValue = (value: string): string =>
  value.replace(/\\/g, '\\\\').replace(/;/g, '\\;').replace(/,/g, '\\,').replace(/\r?\n/g, '\\n');

const line = (name: string, value: string): string => `${name}:${escapeValue(value)}`;

export const buildVCard = (employee: Employee, orgName: string): string => {
  const lines: string[] = ['BEGIN:VCARD', 'VERSION:3.0'];

  lines.push(line('FN', employee.fullName));
  lines.push(`N:${escapeValue(employee.lastName)};${escapeValue(employee.firstName)};;;`);

  if (orgName) {
    lines.push(line('ORG', orgName));
  }
  if (employee.title) {
    lines.push(line('TITLE', employee.title));
  }
  if (employee.phone) {
    lines.push(`TEL;TYPE=WORK,VOICE:${escapeValue(employee.phone)}`);
  }
  if (employee.email) {
    lines.push(`EMAIL;TYPE=WORK:${escapeValue(employee.email)}`);
  }

  const noteParts: string[] = [];
  if (employee.department) {
    noteParts.push(employee.department);
  }
  if (employee.employeeNumber) {
    noteParts.push(`Sicil: ${employee.employeeNumber}`);
  }
  if (noteParts.length > 0) {
    lines.push(line('NOTE', noteParts.join(' — ')));
  }

  lines.push('END:VCARD');

  return lines.join('\r\n');
};

export const downloadVCard = (employee: Employee, orgName: string): void => {
  const text = buildVCard(employee, orgName);
  const blob = new Blob([text], { type: 'text/vcard;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = `${employee.employeeNumber || employee.fullName}.vcf`;
  document.body.appendChild(anchor);
  anchor.click();
  document.body.removeChild(anchor);
  URL.revokeObjectURL(url);
};
