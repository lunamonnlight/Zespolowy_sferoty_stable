// src/composables/useAuth.ts
import { ref, watch } from 'vue'

export interface User {
    id: number
    username: string
    role: string      // 'admin' lub 'user'
    isBlocked: boolean
}

// Próbujemy odczytać zapisanego testera z LocalStorage
const savedUser = localStorage.getItem('testerUser')
const currentUser = ref<User | null>(savedUser ? JSON.parse(savedUser) : null)

// Jeśli currentUser się zmieni, automatycznie zapisz to w LocalStorage
watch(currentUser, (newVal) => {
    if (newVal) {
        localStorage.setItem('testerUser', JSON.stringify(newVal))
    } else {
        localStorage.removeItem('testerUser')
    }
})

export function useAuth() {
    const setTester = (user: User | null) => {
        currentUser.value = user
    }

    return {
        currentUser,
        setTester
    }
}