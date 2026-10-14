export type Role = 'Admin' | 'Mentor' | 'Student';

export interface User {
  userId: number;
  firstName: string;
  lastName: string;
  email: string;
  role: Role;
}

export interface LoginResponse {
  token: string;
  expiresAtUtc: string;
  user: User;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface Assignment {
  mentorStudentId: number;
  mentorId: number;
  mentorName: string;
  studentId: number;
  studentName: string;
  assignedAt: string;
  isActive: boolean;
}

export interface Student {
  studentId: number;
  userId: number;
  studentCode: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  activeAssignment: Assignment | null;
}

export interface Mentor {
  mentorId: number;
  userId: number;
  employeeCode: string;
  specialization: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  assignments: Assignment[];
}

export interface Meeting {
  meetingId: number;
  mentorStudentId: number;
  mentorId: number;
  mentorName: string;
  studentId: number;
  studentName: string;
  meetingAt: string;
  durationMinutes: number | null;
  topic: string;
  notes: string | null;
  feedback: string | null;
  isActive: boolean;
  rowVersion: string;
}

export interface Goal {
  goalId: number;
  mentorStudentId: number;
  mentorId: number;
  mentorName: string;
  studentId: number;
  studentName: string;
  title: string;
  description: string | null;
  targetDate: string | null;
  status: 'Not Started' | 'In Progress' | 'Completed';
  priority: 'Low' | 'Medium' | 'High';
  completedAt: string | null;
  isActive: boolean;
  rowVersion: string;
}

export function isRole(value: unknown): value is Role {
  return value === 'Admin' || value === 'Mentor' || value === 'Student';
}

export function homeUrl(role: Role): string {
  return `/${role.toLowerCase()}/overview`;
}
