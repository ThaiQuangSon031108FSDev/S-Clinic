import http from 'k6/http';
import { sleep, check } from 'k6';

export const options = {
    // 3 giai đoạn mô phỏng tải:
    stages: [
        { duration: '5s', target: 20 },  // Tăng lên 20 user trong 5s
        { duration: '10s', target: 20 }, // Giữ mức 20 user trong 10s
        { duration: '5s', target: 0 },   // Giảm dần xuống 0 user
    ],
    thresholds: {
        http_req_duration: ['p(95)<500'], // 95% request phải hoàn thành < 500ms
        http_req_failed: ['rate<0.05'],   // Tỉ lệ lỗi < 5%
    },
};

export default function() {
    // Gọi thử 1 API GET Invoices (yêu cầu token nhưng k6 vẫn có thể đo được performance của middleware và ASP.NET routing)
    const url = 'https://localhost:7117/api/invoicesapi';
    
    // Nếu có bearer token để test thực tế thì cấu hình ở đây:
    const params = {
        headers: {
            'Content-Type': 'application/json',
            // 'Authorization': 'Bearer YOUR_TEST_TOKEN' 
        },
    };

    const res = http.get(url, params);
    
    check(res, {
        'status is 200 or 401': (r) => r.status === 200 || r.status === 401,
        'response time < 500ms': (r) => r.timings.duration < 500,
    });
    
    sleep(1); // Giả lập người dùng nghĩ 1s trước khi click tiếp
}
