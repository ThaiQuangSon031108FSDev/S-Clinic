/** doctor.js — Vue 3 stubs for Doctor views */
const { createApp } = Vue;
const DoctorDashboard   = { template: `<div class="max-w-4xl mx-auto px-4 py-10"><h1 class="text-3xl font-bold">Doctor Dashboard</h1><p class="text-slate-400 mt-2">Coming soon...</p></div>` };
const PatientList       = { template: `<div class="max-w-4xl mx-auto px-4 py-10"><h1 class="text-3xl font-bold">My Patients</h1></div>` };
const MedicalRecordForm = { props: ['appointmentId'], template: `<div class="max-w-2xl mx-auto px-4 py-10"><h1 class="text-3xl font-bold">Medical Record</h1></div>` };
const ScheduleManager   = { template: `<div class="max-w-4xl mx-auto px-4 py-10"><h1 class="text-3xl font-bold">My Schedule</h1></div>` };
createApp({ components: { DoctorDashboard, PatientList, MedicalRecordForm, ScheduleManager } }).mount('#app');
