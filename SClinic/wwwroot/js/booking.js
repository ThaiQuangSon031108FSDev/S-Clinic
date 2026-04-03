/**
 * booking.js — Vue 3 booking wizard with Sticky Routing (2.2.5)
 *
 * Sticky Routing: nếu bệnh nhân có gói liệu trình đang active,
 * hệ thống tự động khóa bác sĩ = PrimaryDoctorId của gói đó.
 */
const { createApp, ref, computed } = Vue;

const BookingWizard = {
    template: `
    <div class="max-w-2xl mx-auto px-4 py-12 animate-fade-in">
        <h1 class="text-3xl font-bold text-slate-800 mb-8">Đặt lịch khám</h1>

        <!-- ── Sticky routing banner ─────────────────────────────────────── -->
        <div v-if="stickyTreatment"
             class="mb-6 p-4 rounded-xl bg-brand-50 border border-brand-200 text-sm flex items-start gap-3">
            <span class="text-xl">📌</span>
            <div>
                <p class="font-semibold text-brand-800">Gói liệu trình đang hoạt động</p>
                <p class="text-brand-700 mt-1">
                    Bạn đang điều trị gói <strong>{{ stickyTreatment.packageName }}</strong>
                    với Bác sĩ <strong>{{ stickyTreatment.primaryDoctorName }}</strong>
                    ({{ stickyTreatment.usedSessions }}/{{ stickyTreatment.totalSessions }} buổi).
                    Lịch hẹn sẽ được tự động gắn với bác sĩ phụ trách.
                </p>
            </div>
        </div>

        <div class="card-glass p-8 space-y-6">
            <!-- Step indicator -->
            <div class="flex items-center gap-2 mb-6">
                <div v-for="(s, i) in steps" :key="i"
                     :class="['flex items-center justify-center w-8 h-8 rounded-full text-sm font-semibold transition-all',
                               i < currentStep ? 'bg-brand-600 text-white'
                             : i === currentStep ? 'ring-2 ring-brand-400 text-brand-600 bg-white'
                             : 'bg-slate-100 text-slate-400']">
                    {{ i + 1 }}
                </div>
                <span class="text-sm text-slate-500 ml-2">{{ steps[currentStep] }}</span>
            </div>

            <!-- Step 0: Choose doctor & date -->
            <div v-if="currentStep === 0">
                <label class="block text-sm font-medium text-slate-700 mb-1">Chọn bác sĩ</label>
                <!-- Sticky routing: doctor locked -->
                <div v-if="stickyTreatment"
                     class="form-input bg-slate-50 text-slate-700 flex items-center gap-2 cursor-not-allowed">
                    🔒 {{ stickyTreatment.primaryDoctorName }}
                    <span class="text-xs text-slate-400 ml-auto">(Bác sĩ phụ trách gói)</span>
                </div>
                <select v-else v-model="selectedDoctorId" class="form-input" @change="loadSlots">
                    <option value="">-- Chọn bác sĩ --</option>
                    <option v-for="d in doctors" :key="d.doctorId" :value="d.doctorId">
                        {{ d.fullName }} — {{ d.specialty }}
                    </option>
                </select>

                <label class="block text-sm font-medium text-slate-700 mt-4 mb-1">Chọn ngày</label>
                <input type="date" v-model="selectedDate" class="form-input" @change="loadSlots"
                       :min="todayStr" />
            </div>

            <!-- Step 1: Choose time slot -->
            <div v-if="currentStep === 1">
                <p class="text-sm text-slate-500 mb-3">Chọn khung giờ trống:</p>
                <div v-if="loading" class="space-y-2">
                    <div class="skeleton h-10"></div>
                    <div class="skeleton h-10"></div>
                </div>
                <div v-else-if="slots.length === 0" class="text-slate-400 text-sm text-center py-6">
                    Không có khung giờ trống. Vui lòng chọn ngày khác.
                </div>
                <div v-else class="grid grid-cols-3 gap-3">
                    <button v-for="slot in slots" :key="slot.scheduleId"
                            @click="selectedSlot = slot"
                            :class="['p-3 rounded-xl border text-sm font-medium transition-all',
                                     selectedSlot?.scheduleId === slot.scheduleId
                                       ? 'border-brand-500 bg-brand-50 text-brand-700 shadow-sm'
                                       : 'border-slate-200 hover:border-brand-300 hover:bg-brand-50/50']">
                        {{ slot.timeSlot }}
                    </button>
                </div>
            </div>

            <!-- Step 2: Confirm -->
            <div v-if="currentStep === 2" class="space-y-3">
                <h3 class="font-semibold text-slate-800">Xác nhận đặt lịch</h3>
                <div class="p-4 rounded-xl bg-brand-50 border border-brand-100 space-y-2 text-sm">
                    <p><span class="text-slate-500">Bác sĩ:</span>
                       <span class="font-medium ml-2">{{ selectedDoctor?.fullName ?? stickyTreatment?.primaryDoctorName }}</span></p>
                    <p><span class="text-slate-500">Ngày:</span>
                       <span class="font-medium ml-2">{{ formatDate(selectedDate) }}</span></p>
                    <p><span class="text-slate-500">Giờ:</span>
                       <span class="font-medium ml-2">{{ selectedSlot?.timeSlot }}</span></p>
                    <p v-if="stickyTreatment">
                        <span class="text-slate-500">Gói:</span>
                        <span class="font-medium ml-2 text-brand-700">{{ stickyTreatment.packageName }}</span>
                    </p>
                </div>
            </div>

            <!-- Navigation -->
            <div class="flex justify-between pt-4">
                <button v-if="currentStep > 0" @click="currentStep--" class="btn-secondary">
                    ← Quay lại
                </button>
                <div v-else></div>
                <button v-if="currentStep < 2" @click="nextStep" :disabled="!canProceed"
                        class="btn-primary disabled:opacity-50 disabled:cursor-not-allowed">
                    Tiếp theo →
                </button>
                <button v-else @click="confirmBooking" :disabled="booking"
                        class="btn-primary disabled:opacity-50">
                    {{ booking ? 'Đang đặt...' : '✓ Xác nhận' }}
                </button>
            </div>

            <!-- Success -->
            <div v-if="successMsg"
                 class="p-4 rounded-xl bg-green-50 border border-green-200 text-green-700 text-sm text-center">
                {{ successMsg }}
            </div>
        </div>
    </div>
    `,

    data() {
        return {
            steps: ['Chọn bác sĩ & ngày', 'Chọn giờ', 'Xác nhận'],
            currentStep: 0,
            doctors: [],
            slots: [],
            selectedDoctorId: '',
            selectedDate: '',
            selectedSlot: null,
            loading: false,
            booking: false,
            successMsg: '',
            todayStr: new Date().toISOString().split('T')[0],
            // Sticky routing
            stickyTreatment: null,   // active PatientTreatment (if any)
        };
    },

    computed: {
        selectedDoctor() {
            return this.doctors.find(d => d.doctorId === this.selectedDoctorId);
        },
        canProceed() {
            if (this.currentStep === 0) {
                const hasDoctor = this.stickyTreatment || this.selectedDoctorId;
                return hasDoctor && this.selectedDate;
            }
            if (this.currentStep === 1) return !!this.selectedSlot;
            return true;
        },
        // The effective doctor ID — sticky takes priority
        effectiveDoctorId() {
            return this.stickyTreatment?.primaryDoctorId ?? this.selectedDoctorId;
        }
    },

    async mounted() {
        await Promise.all([this.loadDoctors(), this.checkStickyRouting()]);
    },

    methods: {
        // ── Sticky routing: detect if patient has an active treatment ─────────
        async checkStickyRouting() {
            try {
                const res = await fetch('/api/treatments/my-active', { credentials: 'include' });
                if (!res.ok) return;
                const list = await res.json();
                if (list.length > 0) {
                    // Use the first active treatment's primary doctor
                    this.stickyTreatment   = list[0];
                    this.selectedDoctorId  = list[0].primaryDoctorId;
                }
            } catch { /* patient not logged in or no treatments — safe to ignore */ }
        },

        async loadDoctors() {
            try {
                const res = await fetch('/api/doctors', { credentials: 'include' });
                if (res.ok) this.doctors = await res.json();
            } catch { /* ignore */ }
        },

        async loadSlots() {
            const doctorId = this.effectiveDoctorId;
            if (!doctorId || !this.selectedDate) return;
            this.loading = true;
            this.slots   = [];
            this.selectedSlot = null;
            try {
                const res = await fetch(
                    `/api/booking/slots?doctorId=${doctorId}&date=${this.selectedDate}`,
                    { credentials: 'include' }
                );
                if (res.ok) this.slots = await res.json();
            } catch { /* ignore */ }
            this.loading = false;
        },

        nextStep() {
            if (this.canProceed) this.currentStep++;
        },

        async confirmBooking() {
            this.booking = true;
            try {
                const res = await fetch('/Patient/Book', {
                    method: 'POST',
                    credentials: 'include',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        doctorId:           this.effectiveDoctorId,
                        serviceId:          0,
                        date:               this.selectedDate,
                        time:               this.selectedSlot.timeSlot,
                        patientTreatmentId: this.stickyTreatment?.patientTreatmentId ?? null,
                        note:               null,
                    })
                });
                const data = await res.json();
                if (data.success) {
                    this.successMsg = '✅ Đặt lịch thành công! Lịch hẹn của bạn đã được xác nhận.';
                } else {
                    alert(data.message || 'Đặt lịch thất bại.');
                }
            } catch { alert('Lỗi kết nối.'); }
            this.booking = false;
        },

        formatDate(d) {
            return d ? new Date(d).toLocaleDateString('vi-VN') : '';
        }
    }
};

createApp({ components: { BookingWizard } }).mount('#app');
