Route: /dashboard/inventory

# Depolar arası stok transferi

Kenar çubuğundan Stok sayfasına gidin. Sayfa başlığındaki Transfer Fişi düğmesine tıklayarak Stok transfer fişi penceresini açın.

Kaynak depo ve Hedef depo'yu seçin (ikisi farklı olmalıdır). Her satırda ürünü ve transfer miktarını girin; gerekiyorsa Belge no / referans alanını doldurun. Birden fazla ürün için Satır ekle ile satır ekleyin.

Fişi işle'ye tıklayın. Sistem kaynak depodan çıkış, hedef depoya giriş hareketi oluşturur (POST /api/v1/stock/transfer); toplam stok miktarı ve değeri değişmez, yalnızca lokasyon değişir.
