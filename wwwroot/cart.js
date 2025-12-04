// Cart JavaScript functionality
document.addEventListener("DOMContentLoaded", function() {
    // Initialize cart functionality
    initializeCartHandlers();
    updateCartCount();
});

function initializeCartHandlers() {
    // Quantity increase/decrease buttons
    document.querySelectorAll('.qty-increase').forEach(button => {
        button.addEventListener('click', function() {
            const input = this.parentElement.querySelector('.quantity-input');
            const newQuantity = parseInt(input.value) + 1;
            input.value = newQuantity;
            updateCartItemQuantity(input.dataset.cartItemId, newQuantity);
        });
    });

    document.querySelectorAll('.qty-decrease').forEach(button => {
        button.addEventListener('click', function() {
            const input = this.parentElement.querySelector('.quantity-input');
            const newQuantity = Math.max(1, parseInt(input.value) - 1);
            input.value = newQuantity;
            updateCartItemQuantity(input.dataset.cartItemId, newQuantity);
        });
    });

    // Direct quantity input changes
    document.querySelectorAll('.quantity-input').forEach(input => {
        input.addEventListener('change', function() {
            const quantity = Math.max(1, parseInt(this.value) || 1);
            this.value = quantity;
            updateCartItemQuantity(this.dataset.cartItemId, quantity);
        });
    });

    // Remove item buttons
    document.querySelectorAll('.remove-item').forEach(button => {
        button.addEventListener('click', function() {
            const cartItemId = this.dataset.cartItemId;
            if (confirm('Are you sure you want to remove this item from your cart?')) {
                removeCartItem(cartItemId);
            }
        });
    });
}

// Update cart item quantity via AJAX
async function updateCartItemQuantity(cartItemId, quantity) {
    try {
        const response = await axios.put('/api/cart/update', {
            cartItemId: parseInt(cartItemId),
            quantity: quantity
        });

        if (response.data.success) {
            updateCartDisplay();
            showToast('Success', 'Cart updated successfully');
        } else {
            showToast('Error', 'Failed to update cart');
        }
    } catch (error) {
        console.error('Error updating cart:', error);
        showToast('Error', 'Failed to update cart');
    }
}

// Remove item from cart via AJAX
async function removeCartItem(cartItemId) {
    try {
        const response = await axios.delete(`/api/cart/remove/${cartItemId}`);

        if (response.data.success) {
            // Remove the row from the table
            const row = document.querySelector(`tr[data-cart-item-id=\"${cartItemId}\"]`);
            if (row) {
                row.remove();
            }

            updateCartDisplay();
            updateCartCount();
            showToast('Success', 'Item removed from cart');

            // Check if cart is empty
            if (document.querySelectorAll('#cart-items tr').length === 0) {
                location.reload(); // Reload to show empty cart message
            }
        } else {
            showToast('Error', 'Failed to remove item');
        }
    } catch (error) {
        console.error('Error removing item:', error);
        showToast('Error', 'Failed to remove item');
    }
}

// Update cart totals and display
function updateCartDisplay() {
    let subtotal = 0;
    let discount = 0;

    document.querySelectorAll('#cart-items tr').forEach(row => {
        const quantity = parseInt(row.querySelector('.quantity-input').value);
        const unitPrice = parseFloat(row.querySelector('.unit-price').textContent);
        
        // Calculate discount if applicable
        const discountBadge = row.querySelector('.badge.bg-success');
        let itemDiscount = 0;
        if (discountBadge) {
            const discountPercent = parseFloat(discountBadge.textContent.replace('% OFF', '')) / 100;
            itemDiscount = unitPrice * quantity * discountPercent;
        }

        const itemTotal = (unitPrice * quantity) - itemDiscount;
        
        // Update item subtotal display
        row.querySelector('.item-subtotal').textContent = itemTotal.toFixed(2);
        
        subtotal += unitPrice * quantity;
        discount += itemDiscount;
    });

    // Update totals
    const subtotalElement = document.getElementById('cart-subtotal');
    const discountElement = document.getElementById('cart-discount');
    const totalElement = document.getElementById('cart-total');

    if (subtotalElement) subtotalElement.textContent = subtotal.toFixed(2);
    if (discountElement) discountElement.textContent = discount.toFixed(2);
    if (totalElement) totalElement.textContent = (subtotal - discount).toFixed(2);
}

// Update cart count in navigation
async function updateCartCount() {
    try {
        const userDiv = document.getElementById('User');
        if (!userDiv) return;

        const email = userDiv.dataset.email;
        const isCustomer = userDiv.dataset.customer === 'true';

        if (!email || !isCustomer) return;

        const response = await axios.get(`/api/cart/count/${encodeURIComponent(email)}`);
        const count = response.data;

        // Update cart badge
        let badge = document.querySelector('.cart-badge');
        if (count > 0) {
            if (!badge) {
                badge = document.createElement('span');
                badge.className = 'cart-badge badge bg-danger ms-1';
                const cartLink = document.querySelector('a[href*=\"/Cart\"]');
                if (cartLink) {
                    cartLink.appendChild(badge);
                }
            }
            badge.textContent = count;
        } else if (badge) {
            badge.remove();
        }
    } catch (error) {
        console.error('Error updating cart count:', error);
    }
}

// Show toast notification
function showToast(header, message) {
    document.getElementById('toast_header').textContent = header;
    document.getElementById('toast_body').textContent = message;
    const toast = new bootstrap.Toast(document.getElementById('liveToast'));
    toast.show();
}

// Utility function to format numbers with commas
const numberWithCommas = x => x.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");