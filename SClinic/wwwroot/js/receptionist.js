/** receptionist.js — Vue 3 components for Receptionist views */
const { createApp, ref, reactive, computed } = Vue;

/* ──────────────────────────────────────────────────────────────────────────
   Shared auth helper (Cookie-based auth via ASP.NET — no Bearer token needed)
   ────────────────────────────────────────────────────────────────────────── */
function apiFetch(url, opts = {}) {
    return fetch(url, {
        credentials: 'include',    // send session cookies
        headers: { 'Content-Type': 'application/json', ...(opts.headers || {}) },
        ...opts,
    });
}

/* ══════════════════════════════════════════════════════════════════════════
   SessionLogger — Lễ tân: trừ buổi liệu trình + upload ảnh before/after
   ══════════════════════════════════════════════════════════════════════════ */
const SessionLogger = {
    template: `
    <div class="max-w-5xl mx-auto px-4 py-10 space-y-8 animate-fade-in">
        <h1 class="text-3xl font-bold text-slate-800">🗓️ Ghi Nhận Buổi Liệu Trình</h1>

        <!-- ── Tab bar ── -->
        <div class="flex gap-2 border-b border-slate-200">
            <button @click="tab='use'" :class="tabClass('use')">Trừ buổi</button>
            <button @click="tab='sell'" :class="tabClass('sell')">Bán gói mới</button>
        </div>

        <!-- ══════════════════════════════ TAB: Trừ buổi ════════════════════ -->
        <section v-if="tab==='use'" class="space-y-6">
            <!-- Search patient -->
            <div class="card-glass p-6">
                <h2 class="text-base font-semibold text-slate-700 mb-3">🔍 Tìm bệnh nhân</h2>
                <div class="flex gap-3">
                    <input v-model="searchPhone" @keyup.enter="searchPatient"
                           type="tel" placeholder="Số điện thoại bệnh nhân"
                           class="form-input flex-1" />
                    <button @click="searchPatient" :disabled="searching"
                            class="btn-primary min-w-[120px]">
                        {{ searching ? 'Đang tìm…' : 'Tìm kiếm' }}
                    </button>
                </div>
                <p v-if="searchError" class="text-red-500 text-sm mt-2">{{ searchError }}</p>
            </div>

            <!-- Patient info + Active treatments -->
            <div v-if="patient" class="space-y-6">
                <div class="card-glass p-5 flex items-center gap-4">
                    <div class="w-12 h-12 rounded-full bg-brand-100 flex items-center justify-center text-brand-600 text-xl font-bold">
                        {{ patient.fullName[0] }}
                    </div>
                    <div>
                        <p class="font-semibold text-slate-800 text-lg">{{ patient.fullName }}</p>
                        <p class="text-slate-500 text-sm">📞 {{ patient.phone }}</p>
                    </div>
                </div>

                <!-- No active treatments -->
                <div v-if="treatments.length === 0"
                     class="card-glass p-8 text-center text-slate-400">
                    Bệnh nhân không có gói liệu trình đang hoạt động.
                </div>

                <!-- Treatment cards -->
                <div v-for="t in treatments" :key="t.patientTreatmentId"
                     class="card-glass p-6 space-y-4">
                    <!-- Header -->
                    <div class="flex items-start justify-between flex-wrap gap-3">
                        <div>
                            <p class="font-semibold text-slate-800 text-lg">{{ t.packageName }}</p>
                            <p class="text-sm text-slate-500">Bác sĩ phụ trách: {{ t.primaryDoctorName }}</p>
                        </div>
                        <span :class="['px-3 py-1 rounded-full text-xs font-semibold',
                            t.remaining === 0 ? 'bg-red-100 text-red-600' : 'bg-green-100 text-green-700']">
                            {{ t.remaining }} buổi còn lại
                        </span>
                    </div>

                    <!-- Progress bar -->
                    <div>
                        <div class="flex justify-between text-xs text-slate-500 mb-1">
                            <span>Tiến trình</span>
                            <span>{{ t.usedSessions }} / {{ t.totalSessions }} buổi</span>
                        </div>
                        <div class="w-full bg-slate-100 rounded-full h-2.5">
                            <div class="h-2.5 rounded-full transition-all duration-500"
                                 :class="t.remaining === 0 ? 'bg-red-400' : 'bg-brand-500'"
                                 :style="{ width: progressPct(t) + '%' }"></div>
                        </div>
                    </div>

                    <!-- Notes + Action -->
                    <div v-if="t.remaining > 0" class="space-y-3">
                        <textarea v-model="sessionNotes[t.patientTreatmentId]"
                                  placeholder="Ghi chú buổi điều trị (tùy chọn)…"
                                  rows="2" class="form-input resize-none w-full text-sm"></textarea>
                        <button @click="useSession(t)"
                                :disabled="processing[t.patientTreatmentId]"
                                class="btn-primary w-full text-sm">
                            {{ processing[t.patientTreatmentId] ? '⏳ Đang xử lý…' : '✅ Trừ 1 buổi' }}
                        </button>
                    </div>

                    <!-- Upload images after session logged -->
                    <div v-if="loggedSessions[t.patientTreatmentId]"
                         class="border-t border-slate-100 pt-4 space-y-4">
                        <p class="text-sm font-semibold text-slate-700">
                            📸 Tải ảnh Before / After (tùy chọn)
                        </p>
                        <div class="grid grid-cols-2 gap-4">
                            <div v-for="imgSlot in ['before','after']" :key="imgSlot"
                                 class="space-y-2">
                                <label class="text-xs font-medium text-slate-600 capitalize">
                                    {{ imgSlot === 'before' ? 'Ảnh Trước' : 'Ảnh Sau' }}
                                </label>
                                <label class="flex flex-col items-center justify-center h-32
                                             border-2 border-dashed border-slate-200 rounded-xl
                                             cursor-pointer hover:border-brand-400 hover:bg-brand-50/50
                                             transition-all text-slate-400 text-xs gap-2 overflow-hidden">
                                    <img v-if="uploadedUrls[t.patientTreatmentId]?.[imgSlot]"
                                         :src="uploadedUrls[t.patientTreatmentId][imgSlot]"
                                         class="w-full h-full object-cover rounded-xl" />
                                    <template v-else>
                                        <span class="text-2xl">🖼️</span>
                                        <span>JPG / PNG</span>
                                    </template>
                                    <input type="file" accept=".jpg,.jpeg,.png"
                                           class="hidden"
                                           @change="uploadImage($event, t, imgSlot)" />
                                </label>
                                <p v-if="uploadErrors[t.patientTreatmentId]?.[imgSlot]"
                                   class="text-red-500 text-xs">
                                    {{ uploadErrors[t.patientTreatmentId][imgSlot] }}
                                </p>
                            </div>
                        </div>
                    </div>

                    <!-- Timeline toggle -->
                    <button v-if="loggedSessions[t.patientTreatmentId] || t.usedSessions > 0"
                            @click="toggleTimeline(t)"
                            class="text-sm text-brand-600 hover:underline">
                        {{ showTimeline[t.patientTreatmentId] ? '▲ Ẩn timeline' : '▼ Xem timeline before/after' }}
                    </button>
                    <div v-if="showTimeline[t.patientTreatmentId]" class="space-y-3 pt-2">
                        <div v-if="!timelines[t.patientTreatmentId]" class="text-slate-400 text-sm">
                            Đang tải…
                        </div>
                        <div v-for="log in timelines[t.patientTreatmentId]"
                             :key="log.logId"
                             class="border border-slate-100 rounded-xl p-4 space-y-2">
                            <div class="flex justify-between items-center">
                                <span class="text-sm font-semibold text-slate-700">
                                    Buổi #{{ log.sessionNumber }}
                                </span>
                                <span class="text-xs text-slate-400">{{ log.usedDate }}</span>
                            </div>
                            <p v-if="log.sessionNotes" class="text-xs text-slate-500 italic">{{ log.sessionNotes }}</p>
                            <div v-if="log.images.length" class="grid grid-cols-2 gap-2">
                                <img v-for="img in log.images" :key="img.imageId"
                                     :src="img.imageUrl"
                                     class="w-full h-32 object-cover rounded-lg border border-slate-100" />
                            </div>
                            <p v-else class="text-xs text-slate-400">Chưa có ảnh.</p>
                        </div>
                    </div>

                    <!-- Success message -->
                    <div v-if="successMsg[t.patientTreatmentId]"
                         class="flex items-center gap-2 p-3 rounded-xl bg-green-50 border border-green-200 text-green-700 text-sm">
                        ✅ {{ successMsg[t.patientTreatmentId] }}
                    </div>
                </div>
            </div>
        </section>

        <!-- ══════════════════════════════ TAB: Bán gói mới ═════════════════ -->
        <section v-if="tab==='sell'" class="space-y-6">
            <div class="card-glass p-6 space-y-4">
                <h2 class="text-base font-semibold text-slate-700">💳 Bán gói liệu trình mới</h2>

                <!-- Phone lookup -->
                <div class="flex gap-3">
                    <input v-model="sellPhone" @keyup.enter="searchPatientForSell"
                           type="tel" placeholder="Số điện thoại bệnh nhân"
                           class="form-input flex-1" />
                    <button @click="searchPatientForSell" :disabled="sellSearching"
                            class="btn-secondary min-w-[120px]">
                        {{ sellSearching ? 'Đang tìm…' : 'Tra cứu' }}
                    </button>
                </div>
                <p v-if="sellSearchError" class="text-red-500 text-sm">{{ sellSearchError }}</p>

                <template v-if="sellPatient">
                    <div class="p-3 rounded-xl bg-slate-50 border border-slate-200 text-sm">
                        👤 <strong>{{ sellPatient.fullName }}</strong> — {{ sellPatient.phone }}
                    </div>

                    <!-- Package select -->
                    <div>
                        <label class="block text-sm font-medium text-slate-700 mb-1">Chọn gói</label>
                        <select v-model="sellPackageId" class="form-input">
                            <option value="">-- Chọn gói liệu trình --</option>
                            <option v-for="p in packages" :key="p.packageId" :value="p.packageId">
                                {{ p.packageName }} — {{ formatMoney(p.price) }} ({{ p.totalSessions }} buổi)
                            </option>
                        </select>
                    </div>

                    <!-- Doctor select -->
                    <div>
                        <label class="block text-sm font-medium text-slate-700 mb-1">Bác sĩ phụ trách</label>
                        <select v-model="sellDoctorId" class="form-input">
                            <option value="">-- Chọn bác sĩ --</option>
                            <option v-for="d in doctors" :key="d.doctorId" :value="d.doctorId">
                                {{ d.fullName }} — {{ d.specialty }}
                            </option>
                        </select>
                    </div>

                    <!-- Price summary -->
                    <div v-if="selectedPackage"
                         class="p-4 rounded-xl bg-brand-50 border border-brand-100 text-sm space-y-1">
                        <div class="flex justify-between">
                            <span class="text-slate-600">Gói:</span>
                            <span class="font-medium">{{ selectedPackage.packageName }}</span>
                        </div>
                        <div class="flex justify-between">
                            <span class="text-slate-600">Số buổi:</span>
                            <span class="font-medium">{{ selectedPackage.totalSessions }} buổi</span>
                        </div>
                        <div class="flex justify-between border-t border-brand-200 pt-2 mt-2">
                            <span class="font-semibold text-slate-700">Thành tiền (Thu ngay):</span>
                            <span class="font-bold text-brand-700 text-base">{{ formatMoney(selectedPackage.price) }}</span>
                        </div>
                    </div>

                    <button @click="sellPackage" :disabled="!canSell || selling"
                            class="btn-primary w-full disabled:opacity-50 disabled:cursor-not-allowed">
                        {{ selling ? '⏳ Đang tạo…' : '💰 Xác nhận bán & Thu tiền ngay' }}
                    </button>
                    <div v-if="sellSuccess"
                         class="p-4 rounded-xl bg-green-50 border border-green-200 text-green-700 text-sm">
                        ✅ {{ sellSuccess }}
                    </div>
                    <p v-if="sellError" class="text-red-500 text-sm">{{ sellError }}</p>
                </template>
            </div>
        </section>
    </div>
    `,

    data() {
        return {
            tab: 'use',
            // -- Use session tab --
            searchPhone:   '',
            searching:     false,
            searchError:   '',
            patient:       null,
            treatments:    [],
            sessionNotes:  {},    // { [patientTreatmentId]: string }
            processing:    {},    // { [id]: bool }
            successMsg:    {},    // { [id]: string }
            loggedSessions:{},   // { [id]: logId }
            uploadedUrls:  {},    // { [id]: { before: url, after: url } }
            uploadErrors:  {},    // { [id]: { before: err, after: err } }
            showTimeline:  {},    // { [id]: bool }
            timelines:     {},    // { [id]: [] }
            // -- Sell tab --
            sellPhone:     '',
            sellSearching: false,
            sellSearchError:'',
            sellPatient:   null,
            packages:      [],
            doctors:       [],
            sellPackageId: '',
            sellDoctorId:  '',
            selling:       false,
            sellSuccess:   '',
            sellError:     '',
        };
    },

    computed: {
        selectedPackage() {
            return this.packages.find(p => p.packageId === this.sellPackageId) || null;
        },
        canSell() {
            return this.sellPatient && this.sellPackageId && this.sellDoctorId;
        }
    },

    async mounted() {
        await this.loadPackages();
        await this.loadDoctors();
    },

    methods: {
        tabClass(name) {
            return this.tab === name
                ? 'px-4 py-2 text-sm font-semibold text-brand-600 border-b-2 border-brand-500 -mb-px'
                : 'px-4 py-2 text-sm font-medium text-slate-500 hover:text-slate-700';
        },
        progressPct(t) {
            return t.totalSessions ? Math.round((t.usedSessions / t.totalSessions) * 100) : 0;
        },
        formatMoney(n) {
            return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(n);
        },

        // ── Search patient (use-session tab) ────────────────────────────────
        async searchPatient() {
            this.searching    = true;
            this.searchError  = '';
            this.patient      = null;
            this.treatments   = [];
            this.loggedSessions = {};
            try {
                const res = await apiFetch(`/api/treatments/search-patient?phone=${encodeURIComponent(this.searchPhone)}`);
                if (!res.ok) { this.searchError = 'Không tìm thấy bệnh nhân.'; return; }
                const data = await res.json();
                this.patient    = data.patient;
                this.treatments = data.treatments;
            } catch { this.searchError = 'Lỗi kết nối.'; }
            finally  { this.searching = false; }
        },

        // ── Use session ──────────────────────────────────────────────────────
        async useSession(t) {
            const id = t.patientTreatmentId;
            this.processing  = { ...this.processing,  [id]: true };
            this.successMsg  = { ...this.successMsg,  [id]: '' };
            try {
                const res = await apiFetch(`/api/treatments/${id}/use-session`, {
                    method: 'POST',
                    body: JSON.stringify({ notes: this.sessionNotes[id] || null }),
                });
                const data = await res.json();
                if (!res.ok) { alert(data.message || 'Lỗi trừ buổi.'); return; }

                // Update local state
                t.usedSessions = data.usedSessions;
                t.remaining    = data.remaining;
                this.loggedSessions = { ...this.loggedSessions, [id]: data.logId };
                this.uploadedUrls   = { ...this.uploadedUrls,   [id]: {} };
                this.uploadErrors   = { ...this.uploadErrors,   [id]: {} };

                const msg = data.isCompleted
                    ? `Hoàn tất buổi #${data.usedSessions}. Gói đã kết thúc!`
                    : `Đã trừ buổi #${data.usedSessions}. Còn lại ${data.remaining} buổi.`;
                this.successMsg = { ...this.successMsg, [id]: msg };
            } catch { alert('Lỗi kết nối.'); }
            finally  { this.processing = { ...this.processing, [id]: false }; }
        },

        // ── Upload image ─────────────────────────────────────────────────────
        async uploadImage(event, t, label) {
            const id    = t.patientTreatmentId;
            const logId = this.loggedSessions[id];
            const file  = event.target.files[0];
            if (!file || !logId) return;

            const fd = new FormData();
            fd.append('image', file);
            fd.append('label', label);

            try {
                const res = await fetch(`/api/treatments/upload-image/${logId}`, {
                    method: 'POST',
                    credentials: 'include',
                    body: fd,
                });
                const data = await res.json();
                if (!res.ok) {
                    const errs = { ...(this.uploadErrors[id] || {}), [label]: data.message };
                    this.uploadErrors = { ...this.uploadErrors, [id]: errs };
                    return;
                }
                const urls = { ...(this.uploadedUrls[id] || {}), [label]: data.imageUrl };
                this.uploadedUrls = { ...this.uploadedUrls, [id]: urls };
            } catch {
                const errs = { ...(this.uploadErrors[id] || {}), [label]: 'Lỗi upload.' };
                this.uploadErrors = { ...this.uploadErrors, [id]: errs };
            }
        },

        // ── Timeline ─────────────────────────────────────────────────────────
        async toggleTimeline(t) {
            const id = t.patientTreatmentId;
            this.showTimeline = { ...this.showTimeline, [id]: !this.showTimeline[id] };
            if (this.showTimeline[id] && !this.timelines[id]) {
                await this.loadTimeline(id);
            }
        },
        async loadTimeline(id) {
            try {
                const res = await apiFetch(`/api/treatments/${id}/timeline`);
                if (res.ok) this.timelines = { ...this.timelines, [id]: await res.json() };
            } catch { /* ignore */ }
        },

        // ── Sell package tab ─────────────────────────────────────────────────
        async searchPatientForSell() {
            this.sellSearching  = true;
            this.sellSearchError = '';
            this.sellPatient    = null;
            try {
                const res = await apiFetch(`/api/treatments/search-patient?phone=${encodeURIComponent(this.sellPhone)}`);
                if (!res.ok) { this.sellSearchError = 'Không tìm thấy bệnh nhân.'; return; }
                const data = await res.json();
                this.sellPatient = data.patient;
            } catch { this.sellSearchError = 'Lỗi kết nối.'; }
            finally  { this.sellSearching = false; }
        },

        async loadPackages() {
            try {
                const res = await apiFetch('/api/treatments/packages');
                if (res.ok) this.packages = await res.json();
            } catch { /* ignore */ }
        },

        async loadDoctors() {
            try {
                const res = await apiFetch('/api/treatments/doctors');
                if (res.ok) this.doctors = await res.json();
            } catch { /* ignore */ }
        },

        async sellPackage() {
            if (!this.canSell) return;
            this.selling    = true;
            this.sellSuccess = '';
            this.sellError  = '';
            try {
                const res = await apiFetch('/api/treatments/sell', {
                    method: 'POST',
                    body: JSON.stringify({
                        patientId: this.sellPatient.patientId,
                        packageId: this.sellPackageId,
                        doctorId:  this.sellDoctorId,
                    }),
                });
                const data = await res.json();
                if (!res.ok) { this.sellError = data.message || 'Lỗi tạo gói.'; return; }
                this.sellSuccess = `✅ Đã tạo gói "${data.packageName}" (${data.totalSessions} buổi). Hóa đơn #${data.invoiceId} đã ghi nhận đã thanh toán.`;
                // Reset form
                this.sellPhone    = '';
                this.sellPatient  = null;
                this.sellPackageId = '';
                this.sellDoctorId  = '';
            } catch { this.sellError = 'Lỗi kết nối.'; }
            finally  { this.selling = false; }
        },
    }
};

/* ──────────────────────────── Stub components (khác) ─────────────────── */
const ReceptionistDashboard = { template: `<div class="max-w-4xl mx-auto px-4 py-10"><h1 class="text-3xl font-bold">Receptionist Dashboard</h1></div>` };
const AppointmentManager    = { template: `<div class="max-w-4xl mx-auto px-4 py-10"><h1 class="text-3xl font-bold">Appointments</h1></div>` };
const RegisterPatientForm   = { template: `<div class="max-w-xl mx-auto px-4 py-10"><h1 class="text-3xl font-bold">Register Patient</h1></div>` };

createApp({
    components: { ReceptionistDashboard, AppointmentManager, SessionLogger, RegisterPatientForm }
}).mount('#app');
