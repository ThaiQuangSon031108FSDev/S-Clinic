/**
 * patient.js — Vue 3 components for Patient views
 */
const { createApp } = Vue;

const PatientDashboard = {
    props: {
        appointments: { type: Array, default: () => [] },
        treatments:   { type: Array, default: () => [] },
    },
    template: `
    <div class="max-w-6xl mx-auto px-4 py-10 space-y-8 animate-fade-in">
        <h1 class="text-3xl font-bold text-slate-800">Bảng điều khiển bệnh nhân</h1>

        <!-- Upcoming Appointments -->
        <section class="card-glass p-6">
            <h2 class="text-lg font-semibold text-slate-700 mb-4 flex items-center gap-2">
                📅 Lịch hẹn sắp tới
            </h2>
            <div v-if="upcomingAppointments.length === 0" class="text-slate-400 text-sm py-4 text-center">
                Chưa có lịch hẹn nào.
            </div>
            <table v-else class="table-clinic w-full">
                <thead><tr>
                    <th>Ngày</th><th>Giờ</th><th>Bác sĩ</th><th>Trạng thái</th>
                </tr></thead>
                <tbody>
                    <tr v-for="appt in upcomingAppointments" :key="appt.appointmentId">
                        <td>{{ formatDate(appt.schedule?.workDate) }}</td>
                        <td>{{ appt.schedule?.timeSlot }}</td>
                        <td>{{ appt.schedule?.doctor?.fullName }}</td>
                        <td><span :class="badgeClass(appt.status)">{{ appt.status }}</span></td>
                    </tr>
                </tbody>
            </table>
        </section>

        <!-- Active Treatment Packages -->
        <section class="card-glass p-6">
            <h2 class="text-lg font-semibold text-slate-700 mb-4">💊 Gói liệu trình đang điều trị</h2>
            <div v-if="activeTreatments.length === 0" class="text-slate-400 text-sm py-4 text-center">
                Chưa có gói liệu trình nào.
            </div>
            <div class="grid sm:grid-cols-2 gap-4">
                <div v-for="t in activeTreatments" :key="t.patientTreatmentId"
                     class="p-4 rounded-xl border border-slate-100 bg-white hover:shadow-md transition-shadow">
                    <p class="font-semibold text-slate-800">{{ t.package?.packageName }}</p>
                    <p class="text-xs text-slate-500 mt-1">Bác sĩ: {{ t.primaryDoctor?.fullName }}</p>
                    <div class="mt-3">
                        <div class="flex justify-between text-xs text-slate-500 mb-1">
                            <span>Tiến trình</span>
                            <span>{{ t.usedSessions }}/{{ t.totalSessions }} buổi</span>
                        </div>
                        <div class="w-full bg-slate-100 rounded-full h-2">
                            <div class="bg-brand-500 h-2 rounded-full transition-all"
                                 :style="{ width: progressPercent(t) + '%' }"></div>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    </div>
    `,
    computed: {
        upcomingAppointments() {
            return this.appointments
                .filter(a => a.status !== 'Cancelled' && a.status !== 'Completed')
                .slice(0, 5);
        },
        activeTreatments() {
            return this.treatments.filter(t => t.status === 'Active');
        }
    },
    methods: {
        formatDate(d) {
            if (!d) return '—';
            return new Date(d).toLocaleDateString('vi-VN');
        },
        badgeClass(status) {
            const map = {
                Pending:   'badge-pending',
                Confirmed: 'badge-confirmed',
                Completed: 'badge-completed',
                Cancelled: 'badge-cancelled',
            };
            return map[status] || 'badge-pending';
        },
        progressPercent(t) {
            return t.totalSessions ? Math.round((t.usedSessions / t.totalSessions) * 100) : 0;
        }
    }
};

createApp({ components: { PatientDashboard } }).mount('#app');
