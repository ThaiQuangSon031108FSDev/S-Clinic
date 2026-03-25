/**
 * home.js — Vue 3 components for the Home/Index page
 */
const { createApp } = Vue;

const HomeHero = {
    template: `
    <section class="hero-gradient min-h-screen flex flex-col items-center justify-center text-center px-4 py-20">
        <div class="card-glass max-w-2xl w-full mx-auto p-10 space-y-6">
            <h1 class="text-5xl font-bold text-white drop-shadow-md tracking-tight">
                S-Clinic
            </h1>
            <p class="text-xl text-white/90 font-light">
                Hệ thống quản lý phòng khám da liễu chuyên nghiệp
            </p>
            <p class="text-white/75 text-sm">
                Đặt lịch — Theo dõi liệu trình — Thanh toán thông minh
            </p>
            <div class="flex flex-col sm:flex-row gap-3 justify-center pt-4">
                <button
                    @click="openLogin"
                    class="btn-primary bg-white text-brand-700 hover:bg-brand-50 shadow-lg">
                    Đăng nhập ngay
                </button>
            </div>
        </div>

        <!-- Floating stats -->
        <div class="mt-12 grid grid-cols-3 gap-6 max-w-lg">
            <div v-for="stat in stats" :key="stat.label"
                 class="card-glass p-4 text-center rounded-2xl">
                <p class="text-2xl font-bold text-white">{{ stat.value }}</p>
                <p class="text-white/70 text-xs mt-1">{{ stat.label }}</p>
            </div>
        </div>
    </section>
    `,
    data() {
        return {
            stats: [
                { value: '500+', label: 'Bệnh nhân' },
                { value: '15',   label: 'Bác sĩ' },
                { value: '98%',  label: 'Hài lòng' },
            ]
        };
    },
    methods: {
        openLogin() {
            document.getElementById('btn-login-modal')?.click();
        }
    }
};

createApp({ components: { HomeHero } }).mount('#app');
