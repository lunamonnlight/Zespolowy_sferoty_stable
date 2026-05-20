import { createRouter, createWebHistory } from 'vue-router'
import Home from '../views/Home.vue'
import Report from '../views/Report.vue'
import AdminPanel from '../views/AdminPanel.vue'
import UserPanel from '../views/UserPanel.vue' // IMPORT NOWEGO WIDOKU
import PromoCodeComponent from '../components/PromoCodeComponent.vue';
import Search from "../views/Search.vue"

const router = createRouter({
    history: createWebHistory(),
    routes: [
        { path: '/', name: 'Home', component: Home },
        {
            path: '/promo',
            name: 'PromoCodes',
            component: PromoCodeComponent
        },
        { path: '/admin', name: 'Admin', component: AdminPanel },
        { path: '/report', name: 'Report', component: Report },
        { path: '/connections', name: 'Connections', component: Search },
        { path: '/my-account', name: 'UserPanel', component: UserPanel } // DODANA ŚCIEŻKA
    ]
})

router.beforeEach((to, from, next) => {
    const userStr = localStorage.getItem('testerUser')
    const user = userStr ? JSON.parse(userStr) : null

    // Strażnik Admina
    if (to.path === '/admin') {
        if (!user || user.role !== 'admin') {
            return next('/')
        }
    }

    // Strażnik Konta Użytkownika
    if (to.path === '/my-account') {
        if (!user) {
            return next('/')
        }
    }

    next()
})

export default router