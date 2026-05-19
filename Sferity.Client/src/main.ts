import { createApp } from 'vue'
import './style.css'
import App from './App.vue'
import { primevue } from './plugins/primevue'
import router from './routers'
import ToastService from 'primevue/toastservice'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Tag from 'primevue/tag'


const app = createApp(App)
app.use(router)
app.use(primevue)
app.use(ToastService)

app.component('Button', Button)
app.component('InputText', InputText)
app.component('Tag', Tag)

app.mount('#app')