<script setup lang="ts">
interface Props {
  selectedProject?: string | null
}

const props = defineProps<Props>()

const reviews = ref<Array<{
  id: number
  project?: string
  rating: number
  comment: string
  date: Date
}>>([])

const showReviewForm = ref(false)
const rating = ref(0)
const comment = ref('')

function submitReview() {
  if (rating.value === 0) return

  reviews.value.unshift({
    id: Date.now(),
    project: props.selectedProject || undefined,
    rating: rating.value,
    comment: comment.value,
    date: new Date()
  })

  rating.value = 0
  comment.value = ''
  showReviewForm.value = false
}

function formatDate(date: Date) {
  const now = new Date()
  const diff = now.getTime() - date.getTime()
  const hours = Math.floor(diff / 3600000)

  if (hours < 1) return 'Gerade eben'
  if (hours === 1) return 'Vor 1 Stunde'
  if (hours < 24) return `Vor ${hours} Stunden`
  return date.toLocaleDateString('de-DE')
}
</script>

<template>
  <div class="space-y-8">
    <!-- Review Form -->
    <UCard>
      <template #header>
        <div class="flex items-center justify-between">
          <h3 class="font-semibold">
            Review erstellen
          </h3>
          <UButton
            v-if="!showReviewForm"
            icon="i-lucide-plus"
            @click="showReviewForm = true"
          >
            Neues Review
          </UButton>
        </div>
      </template>

      <div v-if="showReviewForm" class="space-y-4">
        <div>
          <label class="text-sm font-medium mb-2 block">
            Bewertung
          </label>
          <div class="flex gap-2">
            <UButton
              v-for="i in 5"
              :key="i"
              :variant="i <= rating ? 'solid' : 'outline'"
              :color="i <= rating ? 'primary' : 'neutral'"
              icon="i-lucide-star"
              @click="rating = i"
            />
          </div>
        </div>

        <div>
          <label class="text-sm font-medium mb-2 block">
            Kommentar
          </label>
          <UTextarea
            v-model="comment"
            placeholder="Wie lief die Arbeit? Was hast du erreicht?"
            :rows="4"
          />
        </div>

        <div class="flex gap-2 justify-end">
          <UButton
            variant="ghost"
            @click="showReviewForm = false"
          >
            Abbrechen
          </UButton>
          <UButton
            :disabled="rating === 0"
            @click="submitReview"
          >
            Speichern
          </UButton>
        </div>
      </div>
    </UCard>

    <!-- Recent Reviews -->
    <UCard>
      <template #header>
        <h3 class="font-semibold">
          Letzte Reviews
        </h3>
      </template>
      <div class="space-y-4">
        <div
          v-for="review in reviews"
          :key="review.id"
          class="p-4 rounded-lg bg-muted/50 space-y-2"
        >
          <div class="flex items-center justify-between">
            <div>
              <p class="font-medium">
                {{ review.project || 'Kein Projekt' }}
              </p>
              <p class="text-xs text-muted-foreground">
                {{ formatDate(review.date) }}
              </p>
            </div>
            <div class="flex gap-1">
              <Icon
                v-for="i in 5"
                :key="i"
                name="i-lucide-star"
                :class="i <= review.rating ? 'text-yellow-500 fill-yellow-500' : 'text-muted'"
              />
            </div>
          </div>
          <p v-if="review.comment" class="text-sm text-muted-foreground">
            {{ review.comment }}
          </p>
        </div>
        <div v-if="reviews.length === 0" class="text-center py-8 text-muted-foreground">
          Noch keine Reviews
        </div>
      </div>
    </UCard>
  </div>
</template>

