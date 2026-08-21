import { useState, useEffect } from 'react'
import api from '@/lib/axios'

export function useTaxRates() {
  const [isvRate, setIsvRate] = useState(0.15)
  const [touristTaxRate, setTouristTaxRate] = useState(0.04)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    api.get('/settings/business')
      .then(({ data }) => {
        if (data.isvRate != null) {
          const rate = Number(data.isvRate)
          setIsvRate(rate > 1 ? rate / 100 : rate)
        }
        if (data.touristTaxRate != null) {
          const rate = Number(data.touristTaxRate)
          setTouristTaxRate(rate > 1 ? rate / 100 : rate)
        }
      })
      .catch((err) => {
        console.warn('Could not fetch business tax settings, using defaults (15% ISV, 4% Tourist Tax)', err)
      })
      .finally(() => setIsLoading(false))
  }, [])

  return { isvRate, touristTaxRate, isLoading }
}

