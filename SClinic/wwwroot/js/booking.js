/**
 * booking.js — Vue 3 booking wizard
 */
const { createApp, ref, computed } = Vue;

const BookingWizard = {
    template: `
    <div class="max-w-2xl mx-auto px-4 py-12 animate-fade-in">
        <h1 class="text-3xl font-bold text-slate-800 mb-8">Đặt lịch khám</h1>

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

            <!-- Step 0: Choose doctor -->
            <div v-if="currentStep === 0">
                <label class="block text-sm font-medium text-slate-700 mb-1">Chọn bác sĩ</label>
                <select v-model="selectedDoctorId" class="form-input" @change="loadSlots">
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
                       <span class="font-medium ml-2">{{ selectedDoctor?.fullName }}</span></p>
                    <p><span class="text-slate-500">Ngày:</span>
                       <span class="font-medium ml-2">{{ formatDate(selectedDate) }}</span></p>
                    <p><span class="text-slate-500">Giờ:</span>
                       <span class="font-medium ml-2">{{ selectedSlot?.timeSlot }}</span></p>
                </div>
            </div>

            <!-- Navigation buttons -->
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

            <!-- Success message -->
            <div v-if="successMsg" class="p-4 rounded-xl bg-green-50 border border-green-200 text-green-700 text-sm text-center">
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
        };
    },
    computed: {
        selectedDoctor() {
            return this.doctors.find(d => d.doctorId === this.selectedDoctorId);
        },
        canProceed() {
            if (this.currentStep === 0) return this.selectedDoctorId && this.selectedDate;
            if (this.currentStep === 1) return !!this.selectedSlot;
            return true;
        }
    },
    mounted() {
        this.loadDoctors();
    },
    methods: {
        async loadDoctors() {
            const res = await fetch('/api/doctors', {
                headers: { Authorization: `Bearer ${window.SClinicAuth.getToken()}` }
            });
            if (res.ok) this.doctors = await res.json();
        },
        async loadSlots() {
            if (!this.selectedDoctorId || !this.selectedDate) return;
            this.loading = true;
            this.slots = [];
            this.selectedSlot = null;
            const res = await fetch(
                `/api/booking/slots?doctorId=${this.selectedDoctorId}&date=${this.selectedDate}`,
                { headers: { Authorization: `Bearer ${window.SClinicAuth.getToken()}` } }
            );
            if (res.ok) this.slots = await res.json();
            this.loading = false;
        },
        nextStep() {
            if (this.canProceed) this.currentStep++;
        },
        async confirmBooking() {
            this.booking = true;
            const res = await fetch('/api/booking/book', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${window.SClinicAuth.getToken()}`
                },
                body: JSON.stringify({ scheduleId: this.selectedSlot.scheduleId })
            });
            this.booking = false;
            if (res.ok) {
                this.successMsg = '✅ Đặt lịch thành công! Chúng tôi sẽ xác nhận sớm nhất.';
            }
        },
        formatDate(d) {
            return d ? new Date(d).toLocaleDateString('vi-VN') : '';
        }
    }
};

createApp({ components: { BookingWizard } }).mount('#app');
