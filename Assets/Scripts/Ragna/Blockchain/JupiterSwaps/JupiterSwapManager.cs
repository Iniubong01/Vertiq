using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; 
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Programs; 
using System.Text;
using TMPro;
using UnityEngine.UI;
using Reown.AppKit.Unity;

public class JupiterSwapManager : MonoBehaviour
{
    [Header("Jupiter Configuration")]
    public string jupiterBaseUrl = "https://api.jup.ag"; 
    public string jupiterApiKey = ""; 

    [Header("Token Configuration")]
    public string wrappedSolMint = "So11111111111111111111111111111111111111112";
    public string usdcMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v"; 
    public string playTokenMint = "PLAYs3GSSadH2q2JLS7djp7yzeT75NK78XgrE5YLrfq"; 
    
    [Header("Fee Configuration")]
    public string platformFeeWallet = ""; 
    [Range(0f, 10f)]
    public float platformFeePercent = 0.75f; 

    [Header("UI References")]
    public TMP_InputField inputAmountField;
    public TMP_InputField outputAmountField;
    public TMP_Dropdown inputTokenDropdown;
    public TMP_Dropdown outputTokenDropdown;
    public TMP_Text inputBalanceText;
    public TMP_Text outputBalanceText;

    public GameObject quoteInfoContainer; 

    [Header("Quote Details UI")]
    public TMP_Text exchangeRateText;
    public TMP_Text priceImpactText;
    public TMP_Text minimumReceivedText;
    public TMP_Text feeText; 
    public TMP_Text inputUsdText;
    public TMP_Text outputUsdText;

    public Button swapButton;
    public Button maxButton;
    public Button reverseButton;
    public NotificationPopup notificationPopup;
    
    [Header("Swap Settings")]
    [Range(0, 1000)]
    public float slippageBps = 250; 
    public float minSwapAmount = 0.001f; 
    public bool debugMode = true;

    private JObject currentQuote; 
    private Dictionary<string, TokenBalance> tokenBalances = new Dictionary<string, TokenBalance>();
    private bool isUpdatingQuote = false;

    private void Start()
    {
        jupiterBaseUrl = jupiterBaseUrl.Trim().TrimEnd('/');
        if (quoteInfoContainer != null) quoteInfoContainer.SetActive(false);

        InitializeEmptyBalances();
        SetupUI();
        StartCoroutine(WaitForWalletAndRefresh());
    }
    
    private System.Collections.IEnumerator WaitForWalletAndRefresh()
    {
        while (WalletConnector.UserPublicKey == null)
        {
            yield return new WaitForSeconds(0.5f);
        }
        RefreshBalances();
        InvokeRepeating(nameof(RefreshBalances), 15f, 15f);
    }

    private void InitializeEmptyBalances()
    {
        tokenBalances["SOL"] = new TokenBalance { mint = wrappedSolMint, balance = 0, decimals = 9 };
        tokenBalances["PLAY"] = new TokenBalance { mint = playTokenMint, balance = 0, decimals = 9 };
        tokenBalances["USDC"] = new TokenBalance { mint = usdcMint, balance = 0, decimals = 6 };
    }

    private void SetupUI()
    {
        if (inputAmountField != null) inputAmountField.onValueChanged.AddListener(OnInputAmountChanged);
        if (swapButton != null) swapButton.onClick.AddListener(ExecuteSwap);
        if (maxButton != null) maxButton.onClick.AddListener(SetMaxAmount);
        if (reverseButton != null) reverseButton.onClick.AddListener(ReverseTokens);
        if (inputTokenDropdown != null) inputTokenDropdown.onValueChanged.AddListener((_) => OnTokenSelectionChanged());
        if (outputTokenDropdown != null) outputTokenDropdown.onValueChanged.AddListener((_) => OnTokenSelectionChanged());

        SetupTokenDropdowns();
    }

    private void SetupTokenDropdowns()
    {
        var options = new List<string> { "SOL", "PLAY", "USDC" };
        if (inputTokenDropdown != null) { inputTokenDropdown.ClearOptions(); inputTokenDropdown.AddOptions(options); inputTokenDropdown.value = 0; }
        if (outputTokenDropdown != null) { outputTokenDropdown.ClearOptions(); outputTokenDropdown.AddOptions(options); outputTokenDropdown.value = 1; }
    }

    #region BALANCE MANAGEMENT
    public async void RefreshBalances()
    {
        if (WalletConnector.UserPublicKey == null) return;
        
        await UpdateTokenBalance(wrappedSolMint, "SOL");
        await UpdateTokenBalance(playTokenMint, "PLAY");
        if (!string.IsNullOrEmpty(usdcMint)) await UpdateTokenBalance(usdcMint, "USDC");
        
        UpdateBalanceDisplays();
        UpdateButtonState(); 
    }

    private async Task UpdateTokenBalance(string mint, string symbol)
    {
        try
        {
            if (mint == wrappedSolMint)
            {
                var result = await Web3.Rpc.GetBalanceAsync(WalletConnector.UserPublicKey);
                if (result.WasSuccessful)
                {
                    double balance = (double)result.Result.Value / 1_000_000_000;
                    tokenBalances[symbol] = new TokenBalance { mint = mint, balance = balance, decimals = 9 };
                }
            }
            else
            {
                var result = await Web3.Rpc.GetTokenAccountsByOwnerAsync(WalletConnector.UserPublicKey, mint, null);
                if (result.WasSuccessful && result.Result.Value.Count > 0)
                {
                    var tokenAccount = result.Result.Value[0];
                    double balance = double.Parse(tokenAccount.Account.Data.Parsed.Info.TokenAmount.UiAmountString);
                    int decimals = tokenAccount.Account.Data.Parsed.Info.TokenAmount.Decimals;
                    tokenBalances[symbol] = new TokenBalance { mint = mint, balance = balance, decimals = decimals };
                }
                else
                {
                     tokenBalances[symbol] = new TokenBalance { mint = mint, balance = 0, decimals = 9 };
                }
            }
        }
        catch { }
    }

    private void UpdateBalanceDisplays()
    {
        string inputToken = GetSelectedToken(inputTokenDropdown);
        string outputToken = GetSelectedToken(outputTokenDropdown);

        if (inputBalanceText != null)
            inputBalanceText.text = tokenBalances.ContainsKey(inputToken) ? $"{tokenBalances[inputToken].balance:0.####} " : "--";

        if (outputBalanceText != null)
            outputBalanceText.text = tokenBalances.ContainsKey(outputToken) ? $"{tokenBalances[outputToken].balance:0.####} " : "--";
    }
    #endregion

    #region UI INTERACTIONS
    private void OnInputAmountChanged(string value)
    {
        UpdateButtonState(); 
        if (isUpdatingQuote) return;
        if (IsValidInput(out float amount)) UpdateQuote();
        else ClearQuoteInfo(); 
    }

    private void OnTokenSelectionChanged()
    {
        UpdateBalanceDisplays();
        UpdateButtonState();
        if (IsValidInput(out float amount)) UpdateQuote();
        else ClearQuoteInfo();
    }

    private void SetMaxAmount()
    {
        string inputToken = GetSelectedToken(inputTokenDropdown);
        if (tokenBalances.TryGetValue(inputToken, out TokenBalance tokenData))
        {
            double maxAmount = tokenData.balance;
            
            // Safety Buffer for SOL
            if (inputToken == "SOL") 
            {
                double gasBuffer = 0.003; 
                if (maxAmount <= gasBuffer) maxAmount = 0;
                else maxAmount = maxAmount - gasBuffer;
            }

            string format = "0." + new string('#', tokenData.decimals);
            inputAmountField.text = maxAmount.ToString(format);
            OnInputAmountChanged(inputAmountField.text);
        }
    }

    private void ReverseTokens()
    {
        if (inputTokenDropdown != null && outputTokenDropdown != null)
        {
            int temp = inputTokenDropdown.value;
            inputTokenDropdown.value = outputTokenDropdown.value;
            outputTokenDropdown.value = temp;
            string tempAmount = inputAmountField.text;
            inputAmountField.text = outputAmountField.text;
            outputAmountField.text = tempAmount;
        }
    }

    private bool IsValidInput(out float amount)
    {
        amount = 0;
        if (string.IsNullOrEmpty(inputAmountField.text)) return false;
        if (!float.TryParse(inputAmountField.text, out amount)) return false;
        if (amount <= 0) return false;

        string inputToken = GetSelectedToken(inputTokenDropdown);
        if (amount < minSwapAmount) return false;
        if (tokenBalances.ContainsKey(inputToken) && amount > tokenBalances[inputToken].balance) return false;

        return true;
    }

    private void UpdateButtonState()
    {
        if (swapButton == null) return;
        TMP_Text btnText = swapButton.GetComponentInChildren<TMP_Text>();
        if (btnText == null) return;

        string inputToken = GetSelectedToken(inputTokenDropdown);
        float amount = 0;
        bool hasInput = float.TryParse(inputAmountField.text, out amount);

        if (!hasInput || amount <= 0) { btnText.text = "Enter Amount"; swapButton.interactable = false; return; }
        if (amount < minSwapAmount) { btnText.text = $"Minimum {minSwapAmount}"; swapButton.interactable = false; return; }
        if (tokenBalances.ContainsKey(inputToken) && amount > tokenBalances[inputToken].balance) { btnText.text = "Insufficient Funds"; swapButton.interactable = false; return; }

        btnText.text = "Swap";
    }
    #endregion

    #region QUOTE & SWAP
    private async void UpdateQuote()
    {
        if (isUpdatingQuote) return;
        isUpdatingQuote = true;

        try
        {
            string inputToken = GetSelectedToken(inputTokenDropdown);
            string outputToken = GetSelectedToken(outputTokenDropdown);
            
            if (inputToken == outputToken || !float.TryParse(inputAmountField.text, out float amount) || amount <= 0)
            {
                isUpdatingQuote = false; ClearQuoteInfo(); return;
            }

            string inputMint = GetTokenMint(inputToken);
            string outputMint = GetTokenMint(outputToken);
            
            int decimals = tokenBalances.ContainsKey(inputToken) ? tokenBalances[inputToken].decimals : 9;
            ulong amountRaw = (ulong)(amount * Math.Pow(10, decimals));

            if(debugMode) Debug.Log($"[Jupiter] Fetching Quote: {amount} {inputToken} -> {outputToken}");
            currentQuote = await GetQuoteWithRetry(inputMint, outputMint, amountRaw);

            if (currentQuote != null) DisplayQuoteInfo();
            else 
            {
                ShowPopup("No Route", "Try different tokens", Color.red);
                ClearQuoteInfo();
            }
        }
        catch (Exception e) 
        { 
            Debug.LogError($"[Jupiter] Quote Error: {e.Message}");
            ClearQuoteInfo(); 
        }
        finally { isUpdatingQuote = false; }
    }

    private async Task<JObject> GetQuoteWithRetry(string inputMint, string outputMint, ulong amountRaw)
    {
        bool isValidFeeWallet = !string.IsNullOrEmpty(platformFeeWallet) && platformFeeWallet.Length > 30;
        int feeBps = isValidFeeWallet ? Mathf.RoundToInt(platformFeePercent * 100) : 0;
        string feeParam = feeBps > 0 ? $"&platformFeeBps={feeBps}" : "";

        string url = $"{jupiterBaseUrl}/swap/v1/quote?inputMint={inputMint}&outputMint={outputMint}&amount={amountRaw}&slippageBps={slippageBps}{feeParam}";
        return await GetQuote(url);
    }

    // [RESTORED] Full Quote Display Logic
    private void DisplayQuoteInfo()
    {
        if (currentQuote == null) return;
        try
        {
            if (quoteInfoContainer != null) quoteInfoContainer.SetActive(true);
            if (swapButton != null) swapButton.interactable = true;

            string inputToken = GetSelectedToken(inputTokenDropdown);
            string outputToken = GetSelectedToken(outputTokenDropdown);

            // 1. Get Decimals
            int inDecimals = tokenBalances.ContainsKey(inputToken) ? tokenBalances[inputToken].decimals : 9;
            int outDecimals = tokenBalances.ContainsKey(outputToken) ? tokenBalances[outputToken].decimals : 9;

            // 2. Parse Amounts
            double inAmount = double.Parse(currentQuote["inAmount"].ToString()) / Math.Pow(10, inDecimals);
            double outAmount = double.Parse(currentQuote["outAmount"].ToString()) / Math.Pow(10, outDecimals);
            double minReceived = double.Parse(currentQuote["otherAmountThreshold"].ToString()) / Math.Pow(10, outDecimals);

            // 3. Update Output Field
            if (outputAmountField != null) outputAmountField.text = outAmount.ToString($"F{Math.Min(6, outDecimals)}");

            // 4. Update Exchange Rate
            if (exchangeRateText != null && inAmount > 0)
            {
                double rate = outAmount / inAmount;
                exchangeRateText.text = $"1 {inputToken} ≈ {rate:F4} {outputToken}";
            }

            // 5. Update Price Impact (with Color)
            double priceImpact = 0;
            if (priceImpactText != null && currentQuote["priceImpactPct"] != null)
            {
                priceImpact = double.Parse(currentQuote["priceImpactPct"].ToString());
                double displayImpact = priceImpact * 100;
                priceImpactText.text = $"{displayImpact:F2}%";
                
                if (displayImpact < 0.1) priceImpactText.color = Color.green;
                else if (displayImpact < 1.0) priceImpactText.color = new Color(1f, 0.64f, 0f); // Orange
                else priceImpactText.color = Color.red;
            }

            // 6. Update Min Received
            if (minimumReceivedText != null) minimumReceivedText.text = $"{minReceived:F4} {outputToken}";

            // 7. Update Fee
            if (feeText != null) feeText.text = $"{platformFeePercent}%";

            // 8. Update USD Values
            if (currentQuote["swapUsdValue"] != null)
            {
                string usdValStr = currentQuote["swapUsdValue"].ToString();
                if (double.TryParse(usdValStr, out double usdVal))
                {
                    if (inputUsdText != null) inputUsdText.text = $"≈ ${usdVal:F2}";
                    if (outputUsdText != null) 
                    {
                        // Estimate output USD by reducing price impact
                        double outputUsdVal = usdVal * (1.0 - priceImpact);
                        outputUsdText.text = $"≈ ${outputUsdVal:F2}";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Jupiter] Display Error: {ex.Message}");
        }
    }

    // [RESTORED] Clears all UI elements
    private void ClearQuoteInfo()
    {
        if (quoteInfoContainer != null) quoteInfoContainer.SetActive(false);
        if (outputAmountField != null) outputAmountField.text = "";
        
        if (exchangeRateText != null) exchangeRateText.text = "--";
        if (priceImpactText != null) priceImpactText.text = "--";
        if (minimumReceivedText != null) minimumReceivedText.text = "--";
        if (feeText != null) feeText.text = "--";
        if (inputUsdText != null) inputUsdText.text = "";
        if (outputUsdText != null) outputUsdText.text = "";

        if (swapButton != null) swapButton.interactable = false;
        UpdateButtonState(); 
    }

    private async void ExecuteSwap()
    {
        if (currentQuote == null || WalletConnector.UserPublicKey == null) return;
        
        swapButton.interactable = false;
        ShowPopup("Creating Transaction", "Please wait...", Color.yellow);

        try
        {
            string txBase64 = await GetSwapTransaction(currentQuote);
            if (string.IsNullOrEmpty(txBase64)) 
            { 
                ShowPopup("Error", "Failed to create transaction", Color.red); 
                swapButton.interactable = true;
                return; 
            }

            ShowPopup("Confirm Swap", "Processing...", Color.yellow);
            await SignAndSendTransaction(txBase64);
        }
        catch (Exception ex) 
        { 
            ShowPopup("Failed", ex.Message, Color.red); 
            swapButton.interactable = true;
        }
    }

    private async void HandleSwapSuccess(string signature)
    {
        ShowPopup("Success!", "Swap Completed!", Color.green);
        if(debugMode) Debug.Log($"[Jupiter] TX Sent: {signature}");
        if (inputAmountField != null) inputAmountField.text = "";
        ClearQuoteInfo();
        await Task.Delay(2000);
        RefreshBalances();
        swapButton.interactable = true;
    }

    private async Task<JObject> GetQuote(string url)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            if(!string.IsNullOrEmpty(jupiterApiKey)) req.SetRequestHeader("x-api-key", jupiterApiKey);
            await req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success) return JObject.Parse(req.downloadHandler.text); 
            else return null;
        }
    }

    private async Task<string> GetSwapTransaction(JObject quote)
    {
        string feeAccount = null;
        if (!string.IsNullOrEmpty(platformFeeWallet) && platformFeePercent > 0)
        {
            try 
            {
                string outputToken = GetSelectedToken(outputTokenDropdown);
                string outputMint = GetTokenMint(outputToken);
                PublicKey feeWalletKey = new PublicKey(platformFeeWallet);
                PublicKey mintKey = new PublicKey(outputMint);
                PublicKey ata = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(feeWalletKey, mintKey);
                feeAccount = ata.ToString();
            }
            catch { }
        }

        // Fixed priority fee (0.00005 SOL) to prevent low-balance errors
        var reqData = new 
        {
            quoteResponse = quote, 
            userPublicKey = WalletConnector.UserPublicKey.ToString(),
            wrapAndUnwrapSol = true,
            dynamicComputeUnitLimit = true, 
            prioritizationFeeLamports = 50000, 
            feeAccount = feeAccount 
        };

        string json = JsonConvert.SerializeObject(reqData, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        
        using (UnityWebRequest req = new UnityWebRequest($"{jupiterBaseUrl}/swap/v1/swap", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            if(!string.IsNullOrEmpty(jupiterApiKey)) req.SetRequestHeader("x-api-key", jupiterApiKey);

            await req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var responseObj = JObject.Parse(req.downloadHandler.text);
                return responseObj["swapTransaction"].ToString();
            }
            else
            {
                Debug.LogError($"[Jupiter] Swap Error: {req.error}");
                return null;
            }
        }
    }

    private async Task SignAndSendTransaction(string txBase64)
    {
        try
        {
            // PATH A: EDITOR
            if (WalletConnector.PlayerAccount != null) 
            {
                byte[] txBytes = Convert.FromBase64String(txBase64);
                // Editor parsing logic
                int offset = 0;
                byte versionByte = txBytes[offset++];
                bool isVersioned = (versionByte & 0x80) != 0;
                
                if (isVersioned)
                {
                    int numSignatures = ReadCompactU16(txBytes, ref offset);
                    int messageStart = offset + (numSignatures * 64);
                    byte[] messageBytes = new byte[txBytes.Length - messageStart];
                    Array.Copy(txBytes, messageStart, messageBytes, 0, messageBytes.Length);
                    byte[] signature = WalletConnector.PlayerAccount.Sign(messageBytes);
                    byte[] signedTx = new byte[1 + GetCompactU16Size(numSignatures) + (signature.Length * numSignatures) + messageBytes.Length];
                    int writeOffset = 0;
                    signedTx[writeOffset++] = versionByte;
                    WriteCompactU16(signedTx, ref writeOffset, numSignatures);
                    Array.Copy(signature, 0, signedTx, writeOffset, signature.Length);
                    writeOffset += signature.Length;
                    Array.Copy(messageBytes, 0, signedTx, writeOffset, messageBytes.Length);
                    await SendSignedTransaction(Convert.ToBase64String(signedTx));
                }
                else
                {
                    int numSignatures = txBytes[0];
                    int messageStart = 1 + (numSignatures * 64);
                    byte[] messageBytes = new byte[txBytes.Length - messageStart];
                    Array.Copy(txBytes, messageStart, messageBytes, 0, messageBytes.Length);
                    byte[] signature = WalletConnector.PlayerAccount.Sign(messageBytes);
                    byte[] signedTx = new byte[1 + signature.Length + messageBytes.Length];
                    signedTx[0] = (byte)numSignatures;
                    Array.Copy(signature, 0, signedTx, 1, signature.Length);
                    Array.Copy(messageBytes, 0, signedTx, 1 + signature.Length, messageBytes.Length);
                    await SendSignedTransaction(Convert.ToBase64String(signedTx));
                }
            } 
            // PATH B: ANDROID
            else if (AppKit.IsInitialized)
            {
                ShowPopup("Wallet", "Please sign in wallet...", Color.yellow);
                var signResponse = await AppKit.Solana.SignTransactionAsync(txBase64);
                if (signResponse != null && !string.IsNullOrEmpty(signResponse.TransactionBase64))
                {
                    ShowPopup("Processing", "Finalizing swap...", Color.yellow);
                    await SendSignedTransaction(signResponse.TransactionBase64);
                }
                else
                {
                    ShowPopup("Cancelled", "Swap cancelled", Color.red);
                    swapButton.interactable = true;
                }
            }
        }
        catch (Exception ex)
        {
            ShowPopup("Error", "Signing Failed", Color.red);
            swapButton.interactable = true;
            Debug.LogError($"[Jupiter] Sign Error: {ex.Message}");
        }
    }

    private async Task SendSignedTransaction(string signedTxBase64)
    {
        using (UnityWebRequest req = new UnityWebRequest(Web3.Rpc.NodeAddress.AbsoluteUri, "POST"))
        {
            var rpcRequest = new { jsonrpc = "2.0", id = 1, method = "sendTransaction", @params = new object[] { signedTxBase64, new { encoding = "base64", skipPreflight = false } } };
            string json = JsonConvert.SerializeObject(rpcRequest);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            await req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                 var response = JObject.Parse(req.downloadHandler.text);
                 if (response["error"] == null) HandleSwapSuccess(response["result"].ToString());
                 else 
                 {
                     ShowPopup("Failed", "Transaction Error", Color.red);
                     Debug.LogError($"RPC Error: {response["error"]}");
                     swapButton.interactable = true;
                 }
            }
            else 
            {
                ShowPopup("Failed", "Network Error", Color.red);
                swapButton.interactable = true;
            }
        }
    }
    #endregion

    // Binary Helpers
    private int ReadCompactU16(byte[] data, ref int offset) { byte first = data[offset++]; if (first <= 0x7f) return first; if (first <= 0xbf) return ((first & 0x3f) << 8) | data[offset++]; return ((first & 0x1f) << 16) | (data[offset++] << 8) | data[offset++]; }
    private int GetCompactU16Size(int val) { if (val <= 0x7f) return 1; if (val <= 0x3fff) return 2; return 3; }
    private void WriteCompactU16(byte[] data, ref int offset, int val) { if (val <= 0x7f) data[offset++] = (byte)val; else if (val <= 0x3fff) { data[offset++] = (byte)(0x80 | (val >> 8)); data[offset++] = (byte)(val & 0xff); } else { data[offset++] = (byte)(0xc0 | (val >> 16)); data[offset++] = (byte)((val >> 8) & 0xff); data[offset++] = (byte)(val & 0xff); } }

    #region HELPERS
    private string GetSelectedToken(TMP_Dropdown d) => d == null ? "SOL" : d.options[d.value].text;
    private string GetTokenMint(string t) => t == "SOL" ? wrappedSolMint : (t == "USDC" ? usdcMint : playTokenMint);
    private void ShowPopup(string t, string m, Color c) { if (notificationPopup != null) notificationPopup.Show(t, m, c); }
    [Serializable] public class TokenBalance { public string mint; public double balance; public int decimals; }
    #endregion
}