/** receptionist.js — Vue 3 stubs for Receptionist views */
const { createApp } = Vue;
const ReceptionistDashboard = { template: `<div class="max-w-4xl mx-auto px-4 py-10"><h1 class="text-3xl font-bold">Receptionist Dashboard</h1><p class="text-slate-400 mt-2">Coming soon...</p></div>` };
const AppointmentManager    = { template: `<div class="max-w-4xl mx-auto px-4 py-10"><h1 class="text-3xl font-bold">Appointments</h1></div>` };
const SessionLogger         = { template: `<div class="max-w-2xl mx-auto px-4 py-10"><h1 class="text-3xl font-bold">Log Session</h1></div>` };
const RegisterPatientForm   = { template: `<div class="max-w-xl mx-auto px-4 py-10"><h1 class="text-3xl font-bold">Register Patient</h1></div>` };
createApp({ components: { ReceptionistDashboard, AppointmentManager, SessionLogger, RegisterPatientForm } }).mount('#app');
